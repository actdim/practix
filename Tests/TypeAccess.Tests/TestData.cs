using System.ComponentModel;

namespace OrthoBits.InterCode.Tests
{	
	public class BaseTestClass
	{
		public int IntField;

		private int _intProperty;
		public int IntProperty
		{
			set
			{
				_intProperty = value;
			}
			get
			{
				return _intProperty;
			}
		}

		public string StringField;
		public object ObjectField;

		private string _stringProperty;
		public string StringProperty
		{
			get
			{
				return _stringProperty;
			}
			set
			{
				_stringProperty = value;				
			}
		}

		private BaseTestClass _refPropertyBase;
		public BaseTestClass RefPropertyBase
		{
			get
			{
				return _refPropertyBase;
			}
			set
			{
				_refPropertyBase = value;				
			}
		}

		public BaseTestClass()
		{
			StringProperty = "BaseStringPropertyInitialValue";
			StringField = "BaseStringFieldInitialValue";
			StringPropertyAuto = "BaseStringPropertyAutoInitialValue";
			IntField = int.MinValue;
		}

		public virtual string StringPropertyAuto { get; set; }
	}

	public class TestClass : BaseTestClass//, INotifyPropertyChanged
	{
		public virtual int IntPropertyAuto { get; set; }

		private BaseTestClass _refProperty;
		public BaseTestClass RefProperty
		{
			get
			{
				return _refProperty;
			}
			set
			{
				_refProperty = value;
				// PropertyChanged.Raise(this, ExpressionHelper.NameOf(() => RefProperty));
			}
		}
		public TestClass()
		{
			_refProperty = new BaseTestClass();
			StringProperty = "StringPropertyInitialValue";
			StringField = "StringFieldInitialValue";
			IntPropertyAuto = 0;
			IntField = int.MaxValue;
		}

		public double MethodWithIntParam(int arg1)
		{
			return (double)arg1 * arg1;
		}

		public string MethodWithRefParam(TestClass arg1)
		{
			return arg1.StringProperty + StringProperty;
		}

		public string MethodWithStringParams(params string[] args)
		{
			return string.Join(", ", args);
		}

		public string MethodWithStringParams2(object stringObject, params string[] args)
		{
			return stringObject.ToString() + ", " + string.Join(", ", args);
		}

		// public event PropertyChangedEventHandler PropertyChanged;
	}
	
	public class Test
	{
		public virtual double TestDouble { get; set; }
		public virtual string TestString { get; set; }		
	}
}
