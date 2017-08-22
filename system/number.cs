// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
namespace System {
    
    using System;
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Text;
    using System.Diagnostics.Contracts;

    // The Number class implements methods for formatting and parsing
    // numeric values. To format and parse numeric values, applications should
    // use the Format and Parse methods provided by the numeric
    // classes (Byte, Int16, Int32, Int64,
    // Single, Double, Currency, and Decimal). Those
    // Format and Parse methods share a common implementation
    // provided by this class, and are thus documented in detail here.
    //
    // Formatting
    //
    // The Format methods provided by the numeric classes are all of the
    // form
    //
    //  public static String Format(XXX value, String format);
    //  public static String Format(XXX value, String format, NumberFormatInfo info);
    //
    // where XXX is the name of the particular numeric class. The methods convert
    // the numeric value to a string using the format string given by the
    // format parameter. If the format parameter is null or
    // an empty string, the number is formatted as if the string "G" (general
    // format) was specified. The info parameter specifies the
    // NumberFormatInfo instance to use when formatting the number. If the
    // info parameter is null or omitted, the numeric formatting information
    // is obtained from the current culture. The NumberFormatInfo supplies
    // such information as the characters to use for decimal and thousand
    // separators, and the spelling and placement of currency symbols in monetary
    // values.
    //
    // Format strings fall into two categories: Standard format strings and
    // user-defined format strings. A format string consisting of a single
    // alphabetic character (A-Z or a-z), optionally followed by a sequence of
    // digits (0-9), is a standard format string. All other format strings are
    // used-defined format strings.
    //
    // A standard format string takes the form Axx, where A is an
    // alphabetic character called the format specifier and xx is a
    // sequence of digits called the precision specifier. The format
    // specifier controls the type of formatting applied to the number and the
    // precision specifier controls the number of significant digits or decimal
    // places of the formatting operation. The following table describes the
    // supported standard formats.
    //
    // C c - Currency format. The number is
    // converted to a string that represents a currency amount. The conversion is
    // controlled by the currency format information of the NumberFormatInfo
    // used to format the number. The precision specifier indicates the desired
    // number of decimal places. If the precision specifier is omitted, the default
    // currency precision given by the NumberFormatInfo is used.
    //
    // D d - Decimal format. This format is
    // supported for integral types only. The number is converted to a string of
    // decimal digits, prefixed by a minus sign if the number is negative. The
    // precision specifier indicates the minimum number of digits desired in the
    // resulting string. If required, the number will be left-padded with zeros to
    // produce the number of digits given by the precision specifier.
    //
    // E e Engineering (scientific) format.
    // The number is converted to a string of the form
    // "-d.ddd...E+ddd" or "-d.ddd...e+ddd", where each
    // 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative, and one digit always precedes the decimal point. The
    // precision specifier indicates the desired number of digits after the decimal
    // point. If the precision specifier is omitted, a default of 6 digits after
    // the decimal point is used. The format specifier indicates whether to prefix
    // the exponent with an 'E' or an 'e'. The exponent is always consists of a
    // plus or minus sign and three digits.
    //
    // F f Fixed point format. The number is
    // converted to a string of the form "-ddd.ddd....", where each
    // 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative. The precision specifier indicates the desired number of
    // decimal places. If the precision specifier is omitted, the default numeric
    // precision given by the NumberFormatInfo is used.
    //
    // G g - General format. The number is
    // converted to the shortest possible decimal representation using fixed point
    // or scientific format. The precision specifier determines the number of
    // significant digits in the resulting string. If the precision specifier is
    // omitted, the number of significant digits is determined by the type of the
    // number being converted (10 for int, 19 for long, 7 for
    // float, 15 for double, 19 for Currency, and 29 for
    // Decimal). Trailing zeros after the decimal point are removed, and the
    // resulting string contains a decimal point only if required. The resulting
    // string uses fixed point format if the exponent of the number is less than
    // the number of significant digits and greater than or equal to -4. Otherwise,
    // the resulting string uses scientific format, and the case of the format
    // specifier controls whether the exponent is prefixed with an 'E' or an
    // 'e'.
    //
    // N n Number format. The number is
    // converted to a string of the form "-d,ddd,ddd.ddd....", where
    // each 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative. Thousand separators are inserted between each group of
    // three digits to the left of the decimal point. The precision specifier
    // indicates the desired number of decimal places. If the precision specifier
    // is omitted, the default numeric precision given by the
    // NumberFormatInfo is used.
    //
    // X x - Hexadecimal format. This format is
    // supported for integral types only. The number is converted to a string of
    // hexadecimal digits. The format specifier indicates whether to use upper or
    // lower case characters for the hexadecimal digits above 9 ('X' for 'ABCDEF',
    // and 'x' for 'abcdef'). The precision specifier indicates the minimum number
    // of digits desired in the resulting string. If required, the number will be
    // left-padded with zeros to produce the number of digits given by the
    // precision specifier.
    //
    // Some examples of standard format strings and their results are shown in the
    // table below. (The examples all assume a default NumberFormatInfo.)
    //
    // Value        Format  Result
    // 12345.6789   C       $12,345.68
    // -12345.6789  C       ($12,345.68)
    // 12345        D       12345
    // 12345        D8      00012345
    // 12345.6789   E       1.234568E+004
    // 12345.6789   E10     1.2345678900E+004
    // 12345.6789   e4      1.2346e+004
    // 12345.6789   F       12345.68
    // 12345.6789   F0      12346
    // 12345.6789   F6      12345.678900
    // 12345.6789   G       12345.6789
    // 12345.6789   G7      12345.68
    // 123456789    G7      1.234568E8
    // 12345.6789   N       12,345.68
    // 123456789    N4      123,456,789.0000
    // 0x2c45e      x       2c45e
    // 0x2c45e      X       2C45E
    // 0x2c45e      X8      0002C45E
    //
    // Format strings that do not start with an alphabetic character, or that start
    // with an alphabetic character followed by a non-digit, are called
    // user-defined format strings. The following table describes the formatting
    // characters that are supported in user defined format strings.
    //
    // 
    // 0 - Digit placeholder. If the value being
    // formatted has a digit in the position where the '0' appears in the format
    // string, then that digit is copied to the output string. Otherwise, a '0' is
    // stored in that position in the output string. The position of the leftmost
    // '0' before the decimal point and the rightmost '0' after the decimal point
    // determines the range of digits that are always present in the output
    // string.
    //
    // # - Digit placeholder. If the value being
    // formatted has a digit in the position where the '#' appears in the format
    // string, then that digit is copied to the output string. Otherwise, nothing
    // is stored in that position in the output string.
    //
    // . - Decimal point. The first '.' character
    // in the format string determines the location of the decimal separator in the
    // formatted value; any additional '.' characters are ignored. The actual
    // character used as a the decimal separator in the output string is given by
    // the NumberFormatInfo used to format the number.
    //
    // , - Thousand separator and number scaling.
    // The ',' character serves two purposes. First, if the format string contains
    // a ',' character between two digit placeholders (0 or #) and to the left of
    // the decimal point if one is present, then the output will have thousand
    // separators inserted between each group of three digits to the left of the
    // decimal separator. The actual character used as a the decimal separator in
    // the output string is given by the NumberFormatInfo used to format the
    // number. Second, if the format string contains one or more ',' characters
    // immediately to the left of the decimal point, or after the last digit
    // placeholder if there is no decimal point, then the number will be divided by
    // 1000 times the number of ',' characters before it is formatted. For example,
    // the format string '0,,' will represent 100 million as just 100. Use of the
    // ',' character to indicate scaling does not also cause the formatted number
    // to have thousand separators. Thus, to scale a number by 1 million and insert
    // thousand separators you would use the format string '#,##0,,'.
    //
    // % - Percentage placeholder. The presence of
    // a '%' character in the format string causes the number to be multiplied by
    // 100 before it is formatted. The '%' character itself is inserted in the
    // output string where it appears in the format string.
    //
    // E+ E- e+ e-   - Scientific notation.
    // If any of the strings 'E+', 'E-', 'e+', or 'e-' are present in the format
    // string and are immediately followed by at least one '0' character, then the
    // number is formatted using scientific notation with an 'E' or 'e' inserted
    // between the number and the exponent. The number of '0' characters following
    // the scientific notation indicator determines the minimum number of digits to
    // output for the exponent. The 'E+' and 'e+' formats indicate that a sign
    // character (plus or minus) should always precede the exponent. The 'E-' and
    // 'e-' formats indicate that a sign character should only precede negative
    // exponents.
    //
    // \ - Literal character. A backslash character
    // causes the next character in the format string to be copied to the output
    // string as-is. The backslash itself isn't copied, so to place a backslash
    // character in the output string, use two backslashes (\\) in the format
    // string.
    //
    // 'ABC' "ABC" - Literal string. Characters
    // enclosed in single or double quotation marks are copied to the output string
    // as-is and do not affect formatting.
    //
    // ; - Section separator. The ';' character is
    // used to separate sections for positive, negative, and zero numbers in the
    // format string.
    //
    // Other - All other characters are copied to
    // the output string in the position they appear.
    //
    // For fixed point formats (formats not containing an 'E+', 'E-', 'e+', or
    // 'e-'), the number is rounded to as many decimal places as there are digit
    // placeholders to the right of the decimal point. If the format string does
    // not contain a decimal point, the number is rounded to the nearest
    // integer. If the number has more digits than there are digit placeholders to
    // the left of the decimal point, the extra digits are copied to the output
    // string immediately before the first digit placeholder.
    //
    // For scientific formats, the number is rounded to as many significant digits
    // as there are digit placeholders in the format string.
    //
    // To allow for different formatting of positive, negative, and zero values, a
    // user-defined format string may contain up to three sections separated by
    // semicolons. The results of having one, two, or three sections in the format
    // string are described in the table below.
    //
    // Sections:
    //
    // One - The format string applies to all values.
    //
    // Two - The first section applies to positive values
    // and zeros, and the second section applies to negative values. If the number
    // to be formatted is negative, but becomes zero after rounding according to
    // the format in the second section, then the resulting zero is formatted
    // according to the first section.
    //
    // Three - The first section applies to positive
    // values, the second section applies to negative values, and the third section
    // applies to zeros. The second section may be left empty (by having no
    // characters between the semicolons), in which case the first section applies
    // to all non-zero values. If the number to be formatted is non-zero, but
    // becomes zero after rounding according to the format in the first or second
    // section, then the resulting zero is formatted according to the third
    // section.
    //
    // For both standard and user-defined formatting operations on values of type
    // float and double, if the value being formatted is a NaN (Not
    // a Number) or a positive or negative infinity, then regardless of the format
    // string, the resulting string is given by the NaNSymbol,
    // PositiveInfinitySymbol, or NegativeInfinitySymbol property of
    // the NumberFormatInfo used to format the number.
    //
    // Parsing
    //
    // The Parse methods provided by the numeric classes are all of the form
    //
    //  public static XXX Parse(String s);
    //  public static XXX Parse(String s, int style);
    //  public static XXX Parse(String s, int style, NumberFormatInfo info);
    //
    // where XXX is the name of the particular numeric class. The methods convert a
    // string to a numeric value. The optional style parameter specifies the
    // permitted style of the numeric string. It must be a combination of bit flags
    // from the NumberStyles enumeration. The optional info parameter
    // specifies the NumberFormatInfo instance to use when parsing the
    // string. If the info parameter is null or omitted, the numeric
    // formatting information is obtained from the current culture.
    //
    // Numeric strings produced by the Format methods using the Currency,
    // Decimal, Engineering, Fixed point, General, or Number standard formats
    // (the C, D, E, F, G, and N format specifiers) are guaranteed to be parseable
    // by the Parse methods if the NumberStyles.Any style is
    // specified. Note, however, that the Parse methods do not accept
    // NaNs or Infinities.
    //
    //This class contains only static members and does not need to be serializable 
    [System.Runtime.CompilerServices.FriendAccessAllowed]
    internal class Number
    {
        private Number() {
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatDecimal(Decimal value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatDouble(double value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatInt32(int value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatUInt32(uint value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatInt64(long value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatUInt64(ulong value, String format, NumberFormatInfo info);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern String FormatSingle(float value, String format, NumberFormatInfo info);
    
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public unsafe static extern Boolean NumberBufferToDecimal(byte* number, ref Decimal value);
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe static extern Boolean NumberBufferToDouble(byte* number, ref Double value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Runtime.CompilerServices.FriendAccessAllowed]
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        internal static extern unsafe string FormatNumberBuffer(byte* number, string format, NumberFormatInfo info, char* allDigits);

        // Constants used by number parsing
        //最长数字内容
        private const Int32 NumberMaxDigits = 50;
        /// <summary>
        /// Int32数字精度
        /// </summary>
        private const Int32 Int32Precision = 10;
        /// <summary>
        /// UInt32数字精度
        /// </summary>
        private const Int32 UInt32Precision = Int32Precision;
        /// <summary>
        /// Int64数字精度
        /// </summary>
        private const Int32 Int64Precision = 19;
        /// <summary>
        /// UInt64数字精度
        /// </summary>
        private const Int32 UInt64Precision = 20;

        // NumberBuffer is a partial wrapper around a stack pointer that maps on to
        // the native NUMBER struct so that it can be passed to native directly. It 
        // must be initialized with a stack Byte * of size NumberBufferBytes.
        // For performance, this structure should attempt to be completely inlined.
        // 
        // It should always be initialized like so:
        //
        // Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
        // NumberBuffer number = new NumberBuffer(numberBufferBytes);
        //
        // For performance, when working on the buffer in managed we use the values in this
        // structure, except for the digits, and pack those values into the byte buffer
        // if called out to managed.
        [System.Runtime.CompilerServices.FriendAccessAllowed]
        internal unsafe struct NumberBuffer {

            /// <summary>
            /// 足够的空间NumberMaxDigit字符+零和3 32位整数和一个指针
            /// </summary>
            public static readonly Int32 NumberBufferBytes = 12 + ((NumberMaxDigits + 1) * 2) + IntPtr.Size;
            /// <summary>
            /// 基地址
            /// </summary>
            [SecurityCritical]
            private Byte * baseAddress;
            /// <summary>
            /// 数字缓存字符串
            /// [SecurityCritical]指定代码或程序集执行安全性关键型操作,用于指针等不安全操作
            /// </summary>
            [SecurityCritical]
            public Char * digits;
            /// <summary>
            /// 精度
            /// </summary>
            public Int32 precision;
            /// <summary>
            /// 规模
            /// </summary>
            public Int32 scale;
            /// <summary>
            /// 是否是负数
            /// </summary>
            public Boolean sign;

            [System.Security.SecurityCritical]  // auto-generated
            public NumberBuffer(Byte* stackBuffer) {
                this.baseAddress = stackBuffer;
                this.digits = (((Char*) stackBuffer) + 6);
                this.precision = 0;
                this.scale = 0;
                this.sign = false;
            }

            /// <summary>
            /// 为数字缓存打包
            /// </summary>
            /// <returns></returns>
            [System.Security.SecurityCritical]  // auto-generated
            public Byte* PackForNative() {
                Int32* baseInteger = (Int32*) baseAddress;
                baseInteger[0] = precision;
                baseInteger[1] = scale;
                baseInteger[2] = sign ? 1 : 0;
                return baseAddress;
            }
        }

        /// <summary>
        /// 十六位数字转化为Int32数字
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int32数字</param>
        /// <returns></returns>
        private static Boolean HexNumberToInt32(ref NumberBuffer number, ref Int32 value) {
            UInt32 passedValue = 0;
            Boolean returnValue = HexNumberToUInt32(ref number, ref passedValue);
            value = (Int32)passedValue;
            return returnValue;
        }

        /// <summary>
        /// 十六位数字转化为Int64数字
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int64数字</param>
        /// <returns>是否转换成功</returns>
        private static Boolean HexNumberToInt64(ref NumberBuffer number, ref Int64 value) {
            UInt64 passedValue = 0;
            Boolean returnValue = HexNumberToUInt64(ref number, ref passedValue);
            value = (Int64)passedValue;
            return returnValue;
        }

        /// <summary>
        /// 十六位数字转化为Int32数字
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int64数字</param>
        /// <returns>是否转换成功</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean HexNumberToUInt32(ref NumberBuffer number, ref UInt32 value) {

            Int32 i = number.scale;
            if (i > UInt32Precision || i < number.precision) { 
                return false;
            }
            Char* p = number.digits;
            Contract.Assert(p != null, "");

            UInt32 n = 0;
            while (--i >= 0) {
                if (n > ((UInt32)0xFFFFFFFF / 16)) {
                    return false;
                }
                n *= 16;
                if (*p != '\0') {
                    UInt32 newN = n;
                    if (*p != '\0') {
                        if (*p >= '0' && *p <= '9') {
                            newN += (UInt32)(*p - '0');
                        }
                        else {
                            if (*p >= 'A' && *p <= 'F') {
                                newN += (UInt32)((*p - 'A') + 10);
                            }
                            else {
                                Contract.Assert(*p >= 'a' && *p <= 'f', "");
                                newN += (UInt32)((*p - 'a') + 10);
                            }
                        }
                        p++;
                    }
                
                    // Detect an overflow here...
                    if (newN < n) { 
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        /// <summary>
        /// 十六位数字转化为Int64数字
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int64数字</param>
        /// <returns>是否转换成功</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean HexNumberToUInt64(ref NumberBuffer number, ref UInt64 value) {

            Int32 i = number.scale;
            if (i > UInt64Precision || i < number.precision) {
                return false;
            }
            Char* p = number.digits;
            Contract.Assert(p != null, "");
            
            UInt64 n = 0;
            while (--i >= 0) {
                if (n > (0xFFFFFFFFFFFFFFFF / 16)) {
                    return false;
                }
                n *= 16;
                if (*p != '\0') {
                     UInt64 newN = n;
                    if (*p != '\0') {
                        if (*p >= '0' && *p <= '9') {
                            newN += (UInt64)(*p - '0');
                        }
                        else {
                            if (*p >= 'A' && *p <= 'F') {
                                newN += (UInt64)((*p - 'A') + 10);
                            }
                            else {
                                Contract.Assert(*p >= 'a' && *p <= 'f', "");
                                newN += (UInt64)((*p - 'a') + 10);
                            }
                        }
                        p++;
                    }
                    
                    // Detect an overflow here...
                    if (newN < n) { 
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        /// <summary>
        /// 是否为空数字
        /// </summary>
        /// <param name="ch">字符</param>
        /// <returns>是否为空</returns>
        private static Boolean IsWhite(char ch) {
            return (((ch) == 0x20) || ((ch) >= 0x09 && (ch) <= 0x0D));
        }

        /// <summary>
        /// 数字转换Int32
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int32数字</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean NumberToInt32(ref NumberBuffer number, ref Int32 value) {

            Int32 i = number.scale;
            //精度不符合，则返回失败
            if (i > Int32Precision || i < number.precision) {
                return false;
            }
            //数字缓存内容指针
            char * p = number.digits;
            Contract.Assert(p != null, "");
            Int32 n = 0;
            while (--i >= 0) {
                if ((UInt32)n > (0x7FFFFFFF / 10)) {
                    return false;
                }
                n *= 10;
                //计算内容数字
                if (*p != '\0') {
                    n += (Int32)(*p++ - '0');
                }
            }
            if (number.sign) {
                n = -n;
                if (n > 0) {
                    return false;
                }
            }
            else {
                if (n < 0) {
                    return false;
                }
            }
            value = n;
            return true;
        }

        /// <summary>
        /// 数字转换Int64
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">Int64</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean NumberToInt64(ref NumberBuffer number, ref Int64 value) {

            Int32 i = number.scale;
            if (i > Int64Precision || i < number.precision) {
                return false;
            }
            char* p = number.digits;
            Contract.Assert(p != null, "");
            Int64 n = 0;
            while (--i >= 0) {
                if ((UInt64)n > (0x7FFFFFFFFFFFFFFF / 10)) {
                    return false;
                }
                n *= 10;
                if (*p != '\0') {
                    n += (Int32)(*p++ - '0');
                }
            }
            if (number.sign) {
                n = -n;
                if (n > 0) {
                    return false;
                }
            }
            else {
                if (n < 0) {
                    return false;
                }
            }
            value = n;
            return true;
        }

        /// <summary>
        /// 数字转UInt32数字
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">UInt32</param>
        /// <returns>是否转换成功</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean NumberToUInt32(ref NumberBuffer number, ref UInt32 value) {

            Int32 i = number.scale;//数字规模
            if (i > UInt32Precision || i < number.precision || number.sign ) {//如果精度不符合，则返回失败
                return false;
            }
            char* p = number.digits;//数字缓存字符串
            Contract.Assert(p != null, "");
            UInt32 n = 0;
            while (--i >= 0) {
                if (n > (0xFFFFFFFF / 10)) {
                    return false;
                }
                n *= 10;
                if (*p != '\0') {
                    UInt32 newN = n + (UInt32)(*p++ - '0');
                    // Detect an overflow here...
                    if (newN < n) {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        /// <summary>
        /// 将数字缓存解析为字符串
        /// </summary>
        /// <param name="number">数字缓存</param>
        /// <param name="value">UInt64</param>
        /// <returns>是否转换成功</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Boolean NumberToUInt64(ref NumberBuffer number, ref UInt64 value) {

            Int32 i = number.scale;
            if (i > UInt64Precision || i < number.precision || number.sign) {
                return false;
            }
            char * p = number.digits;
            Contract.Assert(p != null, "");
            UInt64 n = 0;
            while (--i >= 0) {
                if (n > (0xFFFFFFFFFFFFFFFF / 10)) {
                    return false;
                }
                n *= 10;
                if (*p != '\0') {
                    UInt64 newN = n + (UInt64)(*p++ - '0');
                    // Detect an overflow here...
                    if (newN < n) {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }
        /// <summary>
        /// 匹配字符串(如果相同，则返回p,否则返回空)，下一个
        /// </summary>
        /// <param name="p">字符</param>
        /// <param name="str">字符串</param>
        /// <returns>字符数组</returns>
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static char * MatchChars(char* p, string str) {
            fixed (char* stringPointer = str) {
                return MatchChars(p, stringPointer);
            }
        }
        /// <summary>
        /// 匹配字符串数字
        /// </summary>
        /// <param name="p">字符</param>
        /// <param name="str">字符数组</param>
        /// <returns>字符数组</returns>
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static char * MatchChars(char* p, char* str) {
            Contract.Assert(p != null && str != null, "");
            //如果字符串为空，则返回空指针
            if (*str == '\0') {
                return null;
            }
            for (; (*str != '\0'); p++, str++) {
                if (*p != *str) { //We only hurt the failure case
                    //如果*str和*p同时为空，则继续
                    if ((*str == '\u00A0') && (*p == '\u0020')) {// This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 as a 
                        // space character we use 0x20 space character instead to mean the same.
                        continue;
                    }
                    return null;
                }
            }
            return p;
        }

        /// <summary>
        /// 解析为十进制数
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="options">数字模式</param>
        /// <param name="numfmt">数字模式信息</param>
        /// <returns>浮点数</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Decimal ParseDecimal(String value, NumberStyles options, NumberFormatInfo numfmt) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];//创建byte数组
            NumberBuffer number = new NumberBuffer(numberBufferBytes);//创建数组缓存
            Decimal result = 0;
                        
            StringToNumber(value, options, ref number, numfmt, true);//将字符串转换为数字缓存

            if (!NumberBufferToDecimal(number.PackForNative(), ref result)) {//将数字缓存转换为Decimal
                throw new OverflowException(Environment.GetResourceString("Overflow_Decimal"));
            }
            return result;
        }

        /// <summary>
        /// 解析为浮点数
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="options">数字格式</param>
        /// <param name="numfmt">数字值格式和区域相关性信息</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Double ParseDouble(String value, NumberStyles options, NumberFormatInfo numfmt) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];//初始化数组
            NumberBuffer number = new NumberBuffer(numberBufferBytes);//初始化数字缓存
            Double d = 0;
            
            if (!TryStringToNumber(value, options, ref number, numfmt, false)) {
                //如果我们TryStringToNumber失败,它可能来自我们的一个特殊的字符串。检查三个我们关注和重新抛出如果它不是一个字符串。
                String sTrim = value.Trim();//
                if (sTrim.Equals(numfmt.PositiveInfinitySymbol)) {//如果是正无穷大，则返回Double的正无穷大
                    return Double.PositiveInfinity;
                }
                if (sTrim.Equals(numfmt.NegativeInfinitySymbol)) {//如果是负无穷大，则返回Double的负无穷大
                    return Double.NegativeInfinity;
                }
                if (sTrim.Equals(numfmt.NaNSymbol)) {//如果是IEEE NaN（非数字）值的字符串，则返回非数字值
                    return Double.NaN;
                }
                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));//否则抛出格式异常
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref d)) {//将数字缓存转换为Double
                throw new OverflowException(Environment.GetResourceString("Overflow_Double"));
            }

            return d;//返回double
        }

        /// <summary>
        /// 将字符串解析为Int32
        /// </summary>
        /// <param name="s">字符串</param>
        /// <param name="style">确定数字字符串参数中允许的样式</param>
        /// <param name="info">格式设置或区域性信息</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Int32 ParseInt32(String s, NumberStyles style, NumberFormatInfo info) {//

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];//获取数字缓存中的大小空间
            NumberBuffer number = new NumberBuffer(numberBufferBytes);//构造数字缓存
            Int32 i = 0;
    
            StringToNumber(s, style, ref number, info, false);//将字符串转换为数字缓存，并返回

            if ((style & NumberStyles.AllowHexSpecifier) != 0) {//如果允许格式中包含转换十六位，则转换为十六位数字,否则转换为十进制数字
                if (!HexNumberToInt32(ref number, ref i)) { 
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
                }
            }
            else {
                if (!NumberToInt32(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
                }
            }
            return i;           
        }

        /// <summary>
        /// 将字符串解析为Int64
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="options">确定数字字符串参数中允许的样式</param>
        /// <param name="numfmt">格式设置或区域性信息</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Int64 ParseInt64(String value, NumberStyles options, NumberFormatInfo numfmt) {
            Byte* numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];//获取数字缓存中的大小空间
            NumberBuffer number = new NumberBuffer(numberBufferBytes);//构造数字缓存
            Int64 i = 0;

            StringToNumber(value, options, ref number, numfmt, false);//将字符串转换为数字缓存，并返回

            if ((options & NumberStyles.AllowHexSpecifier) != 0) {//如果允许格式中包含转换十六位，则转换为十六位数字,否则转换为十进制数字
                if (!HexNumberToInt64(ref number, ref i)) {//如果转换失败，则
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
                }
            }
            else {
                if (!NumberToInt64(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
                }
            }
            return i;
        }

        /// <summary>
        /// 转换为数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="options">数字格式</param>
        /// <param name="number">数字缓存</param>
        /// <param name="sb">字符串构建器</param>
        /// <param name="numfmt">字符串格式信息</param>
        /// <param name="parseDecimal">是否转换为小数</param>
        /// <returns></returns>
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static Boolean ParseNumber(ref char * str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal) {

            const Int32 StateSign = 0x0001;//符号信号
            const Int32 StateParens = 0x0002;//父亲信号
            const Int32 StateDigits = 0x0004;//数字信号
            const Int32 StateNonZero = 0x0008;//无零信号
            const Int32 StateDecimal = 0x0010;//十进制信号
            const Int32 StateCurrency = 0x0020;//货币信号

            number.scale = 0;
            number.sign = false;
            string decSep;                  // 来自数字格式信息十进制分隔符
            string groupSep;                // 来自数字格式信息组分隔符
            string currSymbol = null;       // 来自数字格式信息货币分隔符

            // ANSI代码页中使用的替代货币的符号,不能ANSI和Unicode之间往返。
            // 目前,只有ja-JP和ko-KR有非空值(U + 005 c,反斜杠)
            string ansicurrSymbol = null;   // 来自数字格式信息ANSI货币分隔符
            string altdecSep = null;        // 来自数字格式信息十进制分隔符作为十进制小数
            string altgroupSep = null;      // 来自数字格式信息组分隔符作为十进制小数

            Boolean parsingCurrency = false; //是否解析为货币
            if ((options & NumberStyles.AllowCurrencySymbol) != 0) {//数字格式为AllowCurrencySymbol，则将numfmt赋值
                currSymbol = numfmt.CurrencySymbol;
                if (numfmt.ansiCurrencySymbol != null) {
                    ansicurrSymbol = numfmt.ansiCurrencySymbol;
                }
                // The idea here is to match the currency separators and on failure match the number separators to keep the perf of VB's IsNumeric fast.
                // The values of decSep are setup to use the correct relevant separator (currency in the if part and decimal in the else part).
                altdecSep = numfmt.NumberDecimalSeparator;
                altgroupSep = numfmt.NumberGroupSeparator;
                decSep = numfmt.CurrencyDecimalSeparator;
                groupSep = numfmt.CurrencyGroupSeparator;
                parsingCurrency = true;
            }
            else {//否则赋值十进制小数和组分隔
                decSep = numfmt.NumberDecimalSeparator;
                groupSep = numfmt.NumberGroupSeparator;
            }

            Int32 state = 0;//状态，各种状态的标记，通过按位运算符计算得出
            Boolean signflag = false; // 缓存的结果”选项& PARSE_LEADINGSIGN & & !(国家& STATE_SIGN)“为了避免这样做两次
            Boolean bigNumber = (sb != null); // 当提供了StringBuilder我们在的地方使用它。数字字符[50]
            Boolean bigNumberHex = (bigNumber && ((options & NumberStyles.AllowHexSpecifier) != 0));//大数并且是十六进制
            Int32 maxParseDigits = bigNumber ? Int32.MaxValue : NumberMaxDigits;//最大解析数字，如果是大数则设置为0x7fffffff，否则设置为50
              
            char* p = str;
            char ch = *p;
            char* next;

            while (true) {
                // 消除空格,除非我们找到一个信号并不是货币符号。
                // “kr 1231.47”是合法的,但“- 1231.47”不是。
                if (IsWhite(ch) && ((options & NumberStyles.AllowLeadingWhite) != 0) && (((state & StateSign) == 0) || (((state & StateSign) != 0) && (((state & StateCurrency) != 0) || numfmt.numberNegativePattern == 2)))) {
                    // 什么也不做，只增加p指针
                }
                // 如果数字字符串具有前置符号、状态相同,寻找正数符号
                else if ((signflag = (((options & NumberStyles.AllowLeadingSign) != 0) && ((state & StateSign) == 0))) && ((next = MatchChars(p, numfmt.positiveSign)) != null)) {
                    state |= StateSign;//相反的符号信号
                    p = next - 1;//减p指针
                } else if (signflag && (next = MatchChars(p, numfmt.negativeSign)) != null) {//寻找负数指针
                    state |= StateSign;//相反的符号信号
                    number.sign = true;
                    p = next - 1;
                }
                else if (ch == '(' && ((options & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0)) {//寻找数字括号'('
                    state |= StateSign | StateParens;//括号状态
                    number.sign = true;
                }
                //货币分隔符不为空，匹配p指针或ANSI货币分隔符不为空，并匹配p指针
                else if ((currSymbol != null && (next = MatchChars(p, currSymbol)) != null) || (ansicurrSymbol != null && (next = MatchChars(p, ansicurrSymbol)) != null)) {
                    state |= StateCurrency;
                    currSymbol = null;  
                    ansicurrSymbol = null;  
                    // 我们已经找到了货币符号。不应该有更多的货币符号。currSymbol设置为NULL,这样我们不会搜索一遍在以后的代码路径。
                    p = next - 1;
                }
                else {
                    break;
                }
                ch = *++p;
            }
            Int32 digCount = 0;//数字计数
            Int32 digEnd = 0;//数字结束
            while (true) {
                //大于0并小于9或是十六位的a到f
                if ((ch >= '0' && ch <= '9') || (((options & NumberStyles.AllowHexSpecifier) != 0) && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')))) {
                    state |= StateDigits;//赋值状态

                    if (ch != '0' || (state & StateNonZero) != 0 || bigNumberHex) {//不为0或是无零状态或是大数十六位
                        if (digCount < maxParseDigits) {//如果数字计数小于最大解析数字
                            if (bigNumber)//如果是大数，则延长缓存字符
                                sb.Append(ch);
                            else//如果是小数，则设置数字数组
                                number.digits[digCount++] = ch;
                            if (ch != '0' || parseDecimal) {//字符不为0或解析小数，则将数字计数设置为数字结束
                                digEnd = digCount;
                            }
                        }
                        if ((state & StateDecimal) == 0) {//如果不是进制状态，则数字规模加1
                            number.scale++;
                        }
                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0) {//如果不是十进制，数字规模减1
                        number.scale--;
                    }
                }
                else if (((options & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, decSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altdecSep)) != null)) {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((options & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, groupSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altgroupSep)) != null)) {
                    p = next - 1;
                }
                else {
                    break;
                }
                ch = *++p;
            }

            Boolean negExp = false;
            number.precision = digEnd;//确定数字缓存精度
            if (bigNumber)//如果是大数，则在StringDuilder末尾添加结束符，
                sb.Append('\0');
            else//否则数组末端添加结束符
                number.digits[digEnd] = '\0';
            if ((state & StateDigits) != 0) {//如果是数字信号
                if ((ch == 'E' || ch == 'e') && ((options & NumberStyles.AllowExponent) != 0)) {//如果允许使用指数符号，并且以e或E结尾的数字
                    char* temp = p;//定位p指针
                    ch = *++p;//获取字符
                    if ((next = MatchChars(p, numfmt.positiveSign)) != null) {//如果是正数，则将字符设置为'+'的下一个字符
                        ch = *(p = next);
                    }
                    else if ((next = MatchChars(p, numfmt.negativeSign)) != null) {//如果是负数，则将字符设置为'-'的下一个字符，设置为负指数
                        ch = *(p = next);
                        negExp = true;
                    }
                    if (ch >= '0' && ch <= '9') {//如果字符大于0小于9
                        Int32 exp = 0;//指数为0
                        do {
                            exp = exp * 10 + (ch - '0');
                            ch = *++p;
                            if (exp > 1000) {
                                exp = 9999;
                                while (ch >= '0' && ch <= '9') {
                                    ch = *++p;
                                }
                            }
                        } while (ch >= '0' && ch <= '9');
                        if (negExp) {//如果是负指数，则将指数相反
                            exp = -exp;
                        }
                        number.scale += exp;//增加规模
                    }
                    else {
                        p = temp;
                        ch = *p;
                    }
                }
                while (true) {
                    if (IsWhite(ch) && ((options & NumberStyles.AllowTrailingWhite) != 0)) {//吞噬空格，不做处理
                    }
                    else if ((signflag = (((options & NumberStyles.AllowTrailingSign) != 0) && ((state & StateSign) == 0))) && (next = MatchChars(p, numfmt.positiveSign)) != null) {//正数符号
                        state |= StateSign;
                        p = next - 1;
                    } else if (signflag && (next = MatchChars(p, numfmt.negativeSign)) != null) {//清除负数符号
                        state |= StateSign;
                        number.sign = true;
                        p = next - 1;
                    }
                    else if (ch == ')' && ((state & StateParens) != 0)) {//如果清除数字括号')'
                        state &= ~StateParens;
                    }
                    else if ((currSymbol != null && (next = MatchChars(p, currSymbol)) != null) || (ansicurrSymbol != null && (next = MatchChars(p, ansicurrSymbol)) != null))
                    {//清除货币分隔符
                        currSymbol = null;
                        ansicurrSymbol = null;
                        p = next - 1;
                    }
                    else {
                        break;
                    }
                    ch = *++p;//增加p指针
                }
                if ((state & StateParens) == 0) {
                    if ((state & StateNonZero) == 0) {
                        if (!parseDecimal) {
                            number.scale = 0;
                        }
                        if ((state & StateDecimal) == 0) {
                            number.sign = false;
                        }
                    }
                    str = p;
                    return true;
                }
            }
            str = p;
            return false;
        }

        /// <summary>
        /// 解析为Single
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="options">确定数字字符串参数中允许的样式</param>
        /// <param name="numfmt">格式设置或区域性信息</param>
        /// <returns>Single</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Single ParseSingle(String value, NumberStyles options, NumberFormatInfo numfmt) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Double d = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, false)) {
                //If we failed TryStringToNumber, it may be from one of our special strings.
                //Check the three with which we're concerned and rethrow if it's not one of
                //those strings.
                String sTrim = value.Trim();
                if (sTrim.Equals(numfmt.PositiveInfinitySymbol)) {
                    return Single.PositiveInfinity;
                }
                if (sTrim.Equals(numfmt.NegativeInfinitySymbol)) {
                    return Single.NegativeInfinity;
                }
                if (sTrim.Equals(numfmt.NaNSymbol)) {
                    return Single.NaN;
                }
                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref d)) {
                throw new OverflowException(Environment.GetResourceString("Overflow_Single"));
            }
            Single castSingle = (Single)d;
            if (Single.IsInfinity(castSingle)) {
                throw new OverflowException(Environment.GetResourceString("Overflow_Single"));
            }
            return castSingle;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static UInt32 ParseUInt32(String value, NumberStyles options, NumberFormatInfo numfmt) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            UInt32 i = 0;
        
            StringToNumber(value, options, ref number, numfmt, false);

            if ((options & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToUInt32(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
                }
            }
            else {
                if (!NumberToUInt32(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
                }
            }
    
            return i;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static UInt64 ParseUInt64(String value, NumberStyles options, NumberFormatInfo numfmt) {
            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            UInt64 i = 0;

            StringToNumber(value, options, ref number, numfmt, false);
            if ((options & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToUInt64(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
                }
            }
            else {
                if (!NumberToUInt64(ref number, ref i)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
                }
            }
            return i;
        }

        /// <summary>
        /// 字符串转数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="options">数字格式</param>
        /// <param name="number">数字缓存</param>
        /// <param name="info">数字形式信息</param>
        /// <param name="parseDecimal">是否转换为小数</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static void StringToNumber(String str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo info, Boolean parseDecimal) {
    
            if (str == null) {
                throw new ArgumentNullException("String");
            }
            Contract.EndContractBlock();
            Contract.Assert(info != null, "");
            //字符串赋值为指针
            fixed (char* stringPointer = str) {
                char * p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, null, info , parseDecimal) //是否可以转换为数字
                    || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer)))) {
                    //抛出格式异常
                    throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                }
            }
        }
        
        /// <summary>
        /// 是否以零结尾
        /// </summary>
        /// <param name="s">字符串</param>
        /// <param name="index">从第i个字符开始</param>
        /// <returns>如果</returns>
        private static Boolean TrailingZeros(String s, Int32 index) {
            // 为了兼容性，我们需要允许落后于0的数字字符串
            for (int i = index; i < s.Length; i++) {
                if (s[i] != '\0') {
                    return false;
                }
            }
            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseDecimal(String value, NumberStyles options, NumberFormatInfo numfmt, out Decimal result) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
                        
            if (!TryStringToNumber(value, options, ref number, numfmt, true)) {
                return false;
            }

            if (!NumberBufferToDecimal(number.PackForNative(), ref result)) {
                return false;
            }
            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseDouble(String value, NumberStyles options, NumberFormatInfo numfmt, out Double result) {
            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;


            if (!TryStringToNumber(value, options, ref number, numfmt, false)) {
                return false;
            }
            if (!NumberBufferToDouble(number.PackForNative(), ref result)) {
                return false;
            }
            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseInt32(String s, NumberStyles style, NumberFormatInfo info, out Int32 result) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
    
            if (!TryStringToNumber(s, style, ref number, info, false)) {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToInt32(ref number, ref result)) { 
                    return false;
                }
            }
            else {
                if (!NumberToInt32(ref number, ref result)) {
                    return false;
                }
            }
            return true;           
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseInt64(String s, NumberStyles style, NumberFormatInfo info, out Int64 result) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
    
            if (!TryStringToNumber(s, style, ref number, info, false)) {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToInt64(ref number, ref result)) { 
                    return false;
                }
            }
            else {
                if (!NumberToInt64(ref number, ref result)) {
                    return false;
                }
            }
            return true;           
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseSingle(String value, NumberStyles options, NumberFormatInfo numfmt, out Single result) {
            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            Double d = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, false)) {
                return false;
            }
            if (!NumberBufferToDouble(number.PackForNative(), ref d)) {
                return false;
            }
            Single castSingle = (Single)d;
            if (Single.IsInfinity(castSingle)) {
                return false;
            }

            result = castSingle;
            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseUInt32(String s, NumberStyles style, NumberFormatInfo info, out UInt32 result) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
    
            if (!TryStringToNumber(s, style, ref number, info, false)) {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToUInt32(ref number, ref result)) { 
                    return false;
                }
            }
            else {
                if (!NumberToUInt32(ref number, ref result)) {
                    return false;
                }
            }
            return true;           
        }
        /// <summary>
        /// 尝试将字符串解析为UInt64
        /// </summary>
        /// <param name="s">要解析的字符串</param>
        /// <param name="style">数字字符串中允许格式</param>
        /// <param name="info">数字格式设置或区域性信息</param>
        /// <param name="result"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Boolean TryParseUInt64(String s, NumberStyles style, NumberFormatInfo info, out UInt64 result) {

            Byte * numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
    
            if (!TryStringToNumber(s, style, ref number, info, false)) {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0) {
                if (!HexNumberToUInt64(ref number, ref result)) { 
                    return false;
                }
            }
            else {
                if (!NumberToUInt64(ref number, ref result)) {
                    return false;
                }
            }
            return true;           
        }

        /// <summary>
        /// 尝试将字符串解析为数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="options">数字字符串中允许格式</param>
        /// <param name="number">数字缓存</param>
        /// <param name="numfmt">数字格式设置或区域性信息</param>
        /// <param name="parseDecimal">是否解析为十进制数</param>
        /// <returns>是否转换成功</returns>
        internal static Boolean TryStringToNumber(String str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo numfmt, Boolean parseDecimal) {   
            return TryStringToNumber(str, options, ref number, null, numfmt, parseDecimal);
        }

        /// <summary>
        /// 将字符串转换为数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="options">数字字符串中允许格式</param>
        /// <param name="number">数字缓存</param>
        /// <param name="sb">表示可变字符字符串。</param>
        /// <param name="numfmt">数字格式设置或区域性信息</param>
        /// <param name="parseDecimal">是否解析为十进制数</param>
        /// <returns>是否转换成功</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.CompilerServices.FriendAccessAllowed]
        internal unsafe static Boolean TryStringToNumber(String str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal) {   

            if (str == null) {//如果字符串为空
                return false;
            }
            Contract.Assert(numfmt != null, "");

            fixed (char* stringPointer = str) {//转换为字符串
                char * p = stringPointer;//赋值p指针
                if (!ParseNumber(ref p, options, ref number, sb, numfmt, parseDecimal) 
                    || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer)))) {
                    return false;
                }
            }

            return true;
        }
    }
}
