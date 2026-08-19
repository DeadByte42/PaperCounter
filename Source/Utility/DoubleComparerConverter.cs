using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PaperCounter.Utility
{
	public enum Operator
	{
		Equals,
		NotEquals,
		LessThan,
		GreaterThan,
		LessThanOrEquals,
		GreaterThanOrEquals
	}

	public class DoubleComparerConverter<T> : IValueConverter
	{
		public DoubleComparerConverter(double reference, Operator @operator, T trueValue, T falseValue)
		{
			Reference = reference;
			Operator = @operator;
			True = trueValue;
			False = falseValue;
		}

		public double Reference { get; set; }
		public Operator Operator { get; set; }
		public T True { get; set; }
		public T False { get; set; }

		public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool result;
			if (value is double num)
			{
				switch (Operator)
				{
					case Operator.Equals: result = (num == Reference); break;
					case Operator.NotEquals: result = (num != Reference); break;
					case Operator.LessThan: result = (num < Reference); break;
					case Operator.GreaterThan: result = (num > Reference); break;
					case Operator.LessThanOrEquals: result = (num <= Reference); break;
					case Operator.GreaterThanOrEquals: result = (num >= Reference); break;
					default: result = false; break;
				}
				return result?True:False;
			}
			return false;
		}

		public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class DoubleComparerVisibilityConverter : DoubleComparerConverter<Visibility>
	{
		public DoubleComparerVisibilityConverter() :
			base(0, Operator.NotEquals, Visibility.Visible, Visibility.Collapsed)
		{ }
	}
}
