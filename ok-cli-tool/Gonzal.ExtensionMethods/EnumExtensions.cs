namespace Gonzal.ExtensionMethods
{
    using System;

    public static class EnumExtensions
    {
        public static TEnum ToEnum<TEnum>(this string name, TEnum defaultEnum, bool enforceCase = false)
            where TEnum : struct, IConvertible
        {
            var enumType = typeof(TEnum);
            if (!enumType.IsEnum)
                throw new ArgumentException("TEnum is not a System.Enum");

            TEnum result = defaultEnum;
            if (!string.IsNullOrWhiteSpace(name)) {
                object parsedResult;
                result = (Enum.TryParse(enumType, name.Trim(), !enforceCase, out parsedResult))
                    ? (TEnum)parsedResult
                    : defaultEnum;
            }
            return result;
        }

        public static TEnum ToEnum<TEnum>(this int value, TEnum defaultEnum)
            where TEnum : struct, IConvertible
        {
            var enumType = typeof(TEnum);
            if (!enumType.IsEnum)
                throw new ArgumentException("TEnum is not a System.Enum");

            TEnum result = defaultEnum;
            if (Enum.IsDefined(enumType, value))
                result = (TEnum)Enum.ToObject(enumType, value);

            return result;
        }
    }
}
