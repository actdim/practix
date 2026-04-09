using ActDim.Practix;
using ActDim.Practix.TypeAccess.Reflection;
using Ardalis.GuardClauses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters;
using System.Security;
using System.Security.Permissions;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
    using Signature = TupleSignature;

    // DynamicClassFactory
    public class DynamicTypeFactory // sealed
    {
        // NamingHelper.CreateUniqueName
        private static readonly string DYNAMIC_ASSEMBLY_NAME = DynamicCodeManager.GetDynamicName(typeof(DynamicTypeFactory).Namespace);

        private static readonly string DYNAMIC_MODULE_NAME = DynamicCodeManager.GetDynamicName(nameof(DynamicTypeFactory));

        public static readonly DynamicTypeFactory Instance = new DynamicTypeFactory(); // TODO: use Lazy<DynamicTypeFactory>

        static DynamicTypeFactory() { }

        private readonly ModuleBuilder _moduleBuilder;

        private readonly ConcurrentDictionary<Signature, Type> _typeCache;

        private readonly ConcurrentDictionary<Signature, Delegate> _delegateCache;

        [SecurityCritical]
        private DynamicTypeFactory()
        {
            _moduleBuilder = DynamicCodeManager.GetModuleBuilder((DYNAMIC_ASSEMBLY_NAME, DYNAMIC_MODULE_NAME));
            _typeCache = new ConcurrentDictionary<Signature, Type>();
            _delegateCache = new ConcurrentDictionary<Signature, Delegate>();
        }

        // CreateDynamicType
        public Type CreateType(PropertyInfo[] properties)
        {
            return CreateType(properties.ToDictionary(pi => pi.Name, pi => pi.PropertyType));
        }

        public Type CreateType(IDictionary<string, Type> propertyTypeMap)
        {
            return CreateType(propertyTypeMap.Select(pt => new DynamicProperty(pt.Key, pt.Value)).ToArray());
        }

        internal Type CreateType(DynamicProperty[] properties)
        {
            // argument must contain at least 1 property definition
            Guard.Against.NullOrEmpty(properties, nameof(properties));
            var signature = new Signature([.. properties]);
            return _typeCache.GetOrAdd(signature, s =>
            {
                var type = CreateType(s, properties);
                return type;
            });
        }

        private Type CreateType(Signature signature, DynamicProperty[] properties)
        {
            //className
            //"DynamicType"?
            var typeName = "DynamicClass" + signature.GetHashCode().ToString(); // TODO: improve
#if ENABLE_LINQ_PARTIAL_TRUST
            new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
            try
            {
                // see also: https://stackoverflow.com/questions/3862226/how-to-dynamically-create-a-class
                // https://github.com/ValeraT1982/ObjectsComparer
                // https://www.c-sharpcorner.com/article/using-objects-comparer-to-compare-complex-objects-in-c-sharp/
                // https://github.com/ekonbenefits/dynamitey

                var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, typeof(DynamicClass)); //TypeAttributes.Serializable??
                var fields = GenerateProperties(typeBuilder, properties);
                GenerateEquals(typeBuilder, fields);
                GenerateGetHashCode(typeBuilder, fields);
                var result = typeBuilder.CreateTypeInfo();
                return result;
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
                PermissionSet.RevertAssert();
#endif
            }
        }

        private FieldInfo[] GenerateProperties(TypeBuilder typeBuilder, DynamicProperty[] properties)
        {
            var fields = new FieldBuilder[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                var dp = properties[i];
                //"_" + dynamicProperty.Name
                var fb = typeBuilder.DefineField("<" + dp.Name + ">k__BackingField", dp.Type, FieldAttributes.Private); //HasDefault?
                var pb = typeBuilder.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);

                //attributes ^ MethodAttributes.VtableLayoutMask
                //getterBuilder
                var mbGet = typeBuilder.DefineMethod("get_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, //Final? Virtual?
                    dp.Type, Type.EmptyTypes);

                var getterGenerator = mbGet.GetILGenerator();
                getterGenerator.Emit(OpCodes.Ldarg_0);
                getterGenerator.Emit(OpCodes.Ldfld, fb);
                getterGenerator.Emit(OpCodes.Ret);
                //setterBuilder
                var mbSet = typeBuilder.DefineMethod("set_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, //Final? Virtual?
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
                generator.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Ldloc, other);
                generator.Emit(OpCodes.Ldfld, field);
                generator.EmitCall(OpCodes.Callvirt, ct.GetMethod("Equals", new Type[] { ft, ft }), null);
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
                generator.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.EmitCall(OpCodes.Callvirt, ct.GetMethod("GetHashCode", new Type[] { ft }), null);
                generator.Emit(OpCodes.Xor);
            }
            generator.Emit(OpCodes.Ret);
        }

        //Create(Object)Instance
        public T CreateObject<T>(IDictionary<string, object> propertyValues)
        {
            return (T)CreateObject(propertyValues, typeof(T));
        }

        public object CreateObject(IDictionary<string, object> propertyValues, Type type = null)
        {
            //http://msdn.microsoft.com/en-us/library/ms145822.aspx
            //http://msdn.microsoft.com/en-us/library/system.reflection.emit.typebuilder.defineproperty(v=vs.71).aspx

            Guard.Against.NullOrEmpty(propertyValues, nameof(propertyValues));

            var propertyTypeMap = propertyValues.ToDictionary(pair => pair.Key, pair => pair.Value == null ? typeof(object) : pair.Value.GetType());
            if (type == null)
            {
                type = CreateType(propertyTypeMap);
            }

            //var ctor = TypeAccessor.GetConstructor(type);
            //var obj = ctor(); // target
            //var setters = new Dictionary<string, Delegate>();
            //foreach (var pair in propertyValues)
            //{
            //	if (!setters.TryGetValue(pair.Key, out var setter))
            //	{
            //		setter = TypeAccessor.GetPropertySetter(type.GetProperty(pair.Key));
            //		setters.Add(pair.Key, setter);
            //	}
            //	setter.DynamicInvoke(obj, pair.Value);
            //}
            //return obj;

            // fastest solution in this particular case
            var signature = new Signature(new Signature(propertyTypeMap.Cast<object>().Concat(new[] { type }).ToArray()));
            //var signature = new Signature(new Signature(propertyValues.Select(pair => pair.Key).Cast<object>().Concat(new[] { type }).ToArray()));

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
                    Expression.MemberInit(Expression.New(type), bindings.ToArray()), parameterExpressions.ToArray()
                ).Compile();
            });

            var obj = multiSetter.DynamicInvoke(propertyValues.Select(pair => pair.Value).ToArray()); //target
            return obj;
        }
    }
}
