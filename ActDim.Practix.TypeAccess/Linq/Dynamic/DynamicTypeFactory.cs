using ActDim.Practix.Collections;
using ActDim.Practix.TypeAccess.Reflection;
using Ardalis.GuardClauses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using DynamicProperty = (string Name, System.Type Type);

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
    /// <summary>
    /// Factory for generating dynamic types (<see cref="DynamicClass"/>) and instances at runtime with auto-properties, <see cref="object.Equals(object)"/>, and <see cref="object.GetHashCode"/>.
    /// </summary>
    public sealed class DynamicTypeFactory
    {
        private static readonly string DynamicAssemblyName = DynamicCodeManager.GetDynamicName(typeof(DynamicTypeFactory).Namespace);
        private static readonly string DynamicModuleName = DynamicCodeManager.GetDynamicName(nameof(DynamicTypeFactory));

        /// <summary>
        /// Gets the singleton instance of <see cref="DynamicTypeFactory"/>.
        /// </summary>
        public static readonly DynamicTypeFactory Instance = new DynamicTypeFactory();

        static DynamicTypeFactory() { }

        private readonly ModuleBuilder _moduleBuilder;
        private readonly ConcurrentDictionary<CompositeKey, Type> _typeCache;
        private readonly ConcurrentDictionary<CompositeKey, Delegate> _delegateCache;

        private DynamicTypeFactory()
        {
            _moduleBuilder = DynamicCodeManager.GetModuleBuilder(DynamicAssemblyName, DynamicModuleName);
            _typeCache = new ConcurrentDictionary<CompositeKey, Type>();
            _delegateCache = new ConcurrentDictionary<CompositeKey, Delegate>();
        }

        /// <summary>
        /// Creates or retrieves a cached dynamic type defined by the given property definitions.
        /// </summary>
        /// <param name="properties">An array of <see cref="PropertyInfo"/> objects specifying property names and types.</param>
        /// <returns>A dynamically emitted <see cref="Type"/> inheriting from <see cref="DynamicClass"/>.</returns>
        public Type CreateType(PropertyInfo[] properties)
        {
            Guard.Against.NullOrEmpty(properties, nameof(properties));
            return CreateType(properties.ToDictionary(pi => pi.Name, pi => pi.PropertyType));
        }

        /// <summary>
        /// Creates or retrieves a cached dynamic type defined by the given property name and type dictionary.
        /// </summary>
        /// <param name="propertyTypeMap">A dictionary mapping property names to property types.</param>
        /// <returns>A dynamically emitted <see cref="Type"/> inheriting from <see cref="DynamicClass"/>.</returns>
        public Type CreateType(IDictionary<string, Type> propertyTypeMap)
        {
            Guard.Against.NullOrEmpty(propertyTypeMap, nameof(propertyTypeMap));
            return CreateType(propertyTypeMap.Select(pt => (DynamicProperty)(pt.Key, pt.Value)).ToArray());
        }

        internal Type CreateType(DynamicProperty[] properties)
        {
            Guard.Against.NullOrEmpty(properties, nameof(properties));
            var signature = new CompositeKey([.. properties.Cast<object>()]);
            return _typeCache.GetOrAdd(signature, s => CreateTypeInternal(s, properties));
        }

        private Type CreateTypeInternal(CompositeKey signature, DynamicProperty[] properties)
        {
            var typeName = "DynamicClass_" + Math.Abs(signature.GetHashCode()).ToString("X");

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, typeof(DynamicClass));
            var fields = GenerateProperties(typeBuilder, properties);
            GenerateEquals(typeBuilder, fields);
            GenerateGetHashCode(typeBuilder, fields);
            return typeBuilder.CreateTypeInfo();
        }

        private FieldInfo[] GenerateProperties(TypeBuilder typeBuilder, DynamicProperty[] properties)
        {
            var fields = new FieldBuilder[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                var dp = properties[i];
                var fb = typeBuilder.DefineField("<" + dp.Name + ">k__BackingField", dp.Type, FieldAttributes.Private);
                var pb = typeBuilder.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);

                var mbGet = typeBuilder.DefineMethod("get_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    dp.Type, Type.EmptyTypes);

                var getterGenerator = mbGet.GetILGenerator();
                getterGenerator.Emit(OpCodes.Ldarg_0);
                getterGenerator.Emit(OpCodes.Ldfld, fb);
                getterGenerator.Emit(OpCodes.Ret);

                var mbSet = typeBuilder.DefineMethod("set_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null, new[] { dp.Type });

                var setterGenerator = mbSet.GetILGenerator();
                setterGenerator.Emit(OpCodes.Ldarg_0);
                setterGenerator.Emit(OpCodes.Ldarg_1);
                setterGenerator.Emit(OpCodes.Stfld, fb);
                setterGenerator.Emit(OpCodes.Ret);

                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i] = fb;
            }
            return fields;
        }

        private void GenerateEquals(TypeBuilder typeBuilder, FieldInfo[] fields)
        {
            var mb = typeBuilder.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), new Type[] { typeof(object) });

            var generator = mb.GetILGenerator();
            var other = generator.DeclareLocal(typeBuilder);
            var next = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Isinst, typeBuilder);
            generator.Emit(OpCodes.Stloc, other);
            generator.Emit(OpCodes.Ldloc, other);
            generator.Emit(OpCodes.Brtrue_S, next);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(next);

            foreach (var field in fields)
            {
                var ft = field.FieldType;
                var ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                next = generator.DefineLabel();
                var getMethod = ct.GetProperty("Default")?.GetGetMethod();
                var equalsMethod = ct.GetMethod("Equals", new Type[] { ft, ft });

                generator.EmitCall(OpCodes.Call, getMethod, null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Ldloc, other);
                generator.Emit(OpCodes.Ldfld, field);
                generator.EmitCall(OpCodes.Callvirt, equalsMethod, null);
                generator.Emit(OpCodes.Brtrue_S, next);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ret);
                generator.MarkLabel(next);
            }

            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Ret);
        }

        private void GenerateGetHashCode(TypeBuilder typeBuilder, FieldInfo[] fields)
        {
            var mb = typeBuilder.DefineMethod("GetHashCode",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);

            var generator = mb.GetILGenerator();
            generator.Emit(OpCodes.Ldc_I4_0);

            foreach (FieldInfo field in fields)
            {
                var ft = field.FieldType;
                var ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                var getMethod = ct.GetProperty("Default")?.GetGetMethod();
                var hashCodeMethod = ct.GetMethod("GetHashCode", new Type[] { ft });

                generator.EmitCall(OpCodes.Call, getMethod, null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.EmitCall(OpCodes.Callvirt, hashCodeMethod, null);
                generator.Emit(OpCodes.Xor);
            }

            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates an instance of a dynamic type populated with values from the given property values dictionary.
        /// </summary>
        /// <typeparam name="T">The expected object type or base type.</typeparam>
        /// <param name="propertyValues">A dictionary of property names and initial values.</param>
        /// <returns>An instance of the dynamic type populated with property values.</returns>
        public T CreateObject<T>(IDictionary<string, object> propertyValues)
        {
            return (T)CreateObject(propertyValues, typeof(T));
        }

        /// <summary>
        /// Creates an instance of a dynamic type populated with values from the given property values dictionary.
        /// </summary>
        /// <param name="propertyValues">A dictionary of property names and initial values.</param>
        /// <param name="type">An optional explicit type to instantiate. If null, a dynamic type is generated.</param>
        /// <returns>An instance of the dynamic type populated with property values.</returns>
        public object CreateObject(IDictionary<string, object> propertyValues, Type type = null)
        {
            Guard.Against.NullOrEmpty(propertyValues, nameof(propertyValues));

            var propertyTypeMap = propertyValues.ToDictionary(
                pair => pair.Key,
                pair => pair.Value == null ? typeof(object) : pair.Value.GetType()
            );

            if (type == null)
            {
                type = CreateType(propertyTypeMap);
            }

            var signature = new CompositeKey([.. propertyTypeMap.Cast<object>(), type]);

            var multiSetter = _delegateCache.GetOrAdd(signature, s =>
            {
                var bindings = new List<MemberBinding>();
                var parameterExpressions = new List<ParameterExpression>();

                foreach (var pair in propertyTypeMap)
                {
                    var parameterExpression = Expression.Parameter(pair.Value, pair.Key);
                    parameterExpressions.Add(parameterExpression);
                    bindings.Add(Expression.Bind(type.GetProperty(pair.Key), parameterExpression));
                }

                return Expression.Lambda(
                    Expression.MemberInit(Expression.New(type), bindings.ToArray()),
                    parameterExpressions.ToArray()
                ).Compile();
            });

            return multiSetter.DynamicInvoke(propertyValues.Select(pair => pair.Value).ToArray());
        }
    }
}
