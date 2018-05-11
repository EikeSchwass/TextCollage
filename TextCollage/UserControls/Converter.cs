using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TextCollage.UserControls
{
    public class VisiblityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            if (parameter != null && parameter.ToString() != "true")
            {
                if (parameter.ToString().ToLower() != "true")
                    boolValue = !boolValue;
            }
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class BoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            if (parameter != null && parameter.ToString() == "true")
                boolValue = !boolValue;

            return boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            if (parameter != null && parameter.ToString() == "true")
                boolValue = !boolValue;

            return boolValue;
        }

        #endregion
    }
}