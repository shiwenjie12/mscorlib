// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:  NumberStyles.cs
**
**
** Purpose: Contains valid formats for Numbers recognized by
** the Number class' parsing code.
**
**
===========================================================*/
namespace System.Globalization {
    
    using System;
    /// <summary>
    /// 确定数字字符串参数中允许的样式，这些参数已传递到整数和浮点数类型的 Parse 和 TryParse 方法。
    /// </summary>
    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum NumberStyles {
        // Bit flag indicating that leading whitespace is allowed. Character values
        // 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, and 0x0020 are considered to be
        // whitespace.
        /// <summary>
        /// 无
        /// </summary>
        None                  = 0x00000000, 
        /// <summary>
        /// 指示所分析的字符串中可以存在前导空白字符
        /// </summary>
        AllowLeadingWhite     = 0x00000001, 
        /// <summary>
        /// 指示所分析的字符串中可以存在结尾空白字符
        /// </summary>
        AllowTrailingWhite    = 0x00000002, //Bitflag indicating trailing whitespace is allowed.
        /// <summary>
        /// 指示数字字符串可以具有前导符号。
        /// </summary>
        AllowLeadingSign      = 0x00000004, //Can the number start with a sign char.  
                                            //Specified by NumberFormatInfo.PositiveSign and NumberFormatInfo.NegativeSign
        /// <summary>
        /// 指示数字字符串可以具有结尾符号。
        /// </summary>
        AllowTrailingSign     = 0x00000008, //Allow the number to end with a sign char

        /// <summary>
        /// 指示数字字符串可以具有一对将数字括起来的括号。
        /// </summary>
        AllowParentheses      = 0x00000010, //Allow the number to be enclosed in parens
        /// <summary>
        /// 指示数字字符串可以具有小数点。
        /// </summary>
        AllowDecimalPoint     = 0x00000020, //Allow a decimal point
        /// <summary>
        /// 指示数字字符串可以具有组分隔符，例如将百位与千位分隔开来的符号。
        /// </summary>
        AllowThousands        = 0x00000040, //Allow thousands separators (more properly, allow group separators)
        /// <summary>
        /// 指示数字字符串用于指数符号中。
        /// </summary>
        AllowExponent         = 0x00000080, //Allow an exponent
        /// <summary>
        /// 指示数字字符串可包含货币符号。
        /// </summary>
        AllowCurrencySymbol   = 0x00000100, //Allow a currency symbol.
        /// <summary>
        /// 指示数值字符串表示一个十六进制值。
        /// </summary>
        AllowHexSpecifier     = 0x00000200, //Allow specifiying hexadecimal.
        //Common uses.  These represent some of the most common combinations of these flags.
    
        /// <summary>
        /// 指示使用 AllowLeadingWhite、AllowTrailingWhite 和 AllowLeadingSign 样式。 这是复合数字样式。
        /// </summary>
        Integer  = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign,
        /// <summary>
        /// 指示使用 AllowLeadingWhite、AllowTrailingWhite 和 AllowHexSpecifier 样式。 这是复合数字样式。 
        /// </summary>
        HexNumber = AllowLeadingWhite | AllowTrailingWhite | AllowHexSpecifier,
        /// <summary>
        /// 指示使用 AllowLeadingWhite、AllowTrailingWhite、AllowLeadingSign、AllowTrailingSign、AllowDecimalPoint 和 AllowThousands 样式。 这是复合数字样式。 
        /// </summary>
        Number   = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowDecimalPoint | AllowThousands,
        /// <summary>
        /// 指示使用 AllowLeadingWhite、AllowTrailingWhite、AllowLeadingSign、AllowDecimalPoint 和 AllowExponent 样式。 这是复合数字样式。 
        /// </summary>
        Float    = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | 
                   AllowDecimalPoint | AllowExponent,
        /// <summary>
        /// 指示使用除 AllowExponent 和 AllowHexSpecifier 以外的所有样式。 这是复合数字样式。 
        /// </summary>
        Currency = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowParentheses  | AllowDecimalPoint | AllowThousands | AllowCurrencySymbol,
        /// <summary>
        /// 指示使用除 AllowHexSpecifier 以外的所有样式。 这是复合数字样式。
        /// </summary>
        Any      = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowParentheses  | AllowDecimalPoint | AllowThousands | AllowCurrencySymbol | AllowExponent,
             
    }
}
