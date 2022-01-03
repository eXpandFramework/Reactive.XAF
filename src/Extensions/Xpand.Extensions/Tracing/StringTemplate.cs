using System;
using System.Collections.Generic;
using System.Text;

namespace Xpand.Extensions.Tracing;

/// <summary>
/// Formats structured strings similar to a composite format string 
/// but using a template with named, rather than positional, arguments.
/// </summary>
public static class StringTemplate {
    // TODO: Add support for objects (in particular anonymous objects)
    // as the source of properties

    // TODO: Allow instances to be created (make the class non-static),
    // that prepare the template in advance and allow fast binding.
    // Maybe something similar to Regex compiled?

    /*
    string _template;

    public StringTemplate(string template)
    {
        _template = template;
    }

    public string Bind(StringTemplate.GetValue getValue) 
    {
        return Format(null, _template, getValue);
    }

    public string Bind(IDictionary<string,object> arguments)
    {
        return Format(null, _template, arguments.TryGetValue);
    }

    public string Bind(IFormatProvider provider, StringTemplate.GetValue getValue)
    {
        return Format(provider, _template, getValue);
    }

    public string Bind(IFormatProvider provider, IDictionary<string, object> arguments)
    {
        return Format(provider, _template, arguments.TryGetValue);
    }
    */

    /// <summary>
    /// Replaces named template items in a specified string 
    /// with the string representation of a corresponding named object 
    /// from the specified dictionary. 
    /// A specified parameter supplies culture-specific formatting information.
    /// </summary>
    /// <param name="template">A template string (see Remarks).</param>
    /// <param name="arguments">A dictionary that contains named objects to format.</param>
    /// <returns>A copy of template in which the template items have been replaced 
    /// by the string representation of the corresponding objects from arguments.</returns>
    /// <exception cref="ArgumentNullException">template or arguments is null.</exception>
    /// <exception cref="FormatException">template is invalid, 
    /// or one of the named template items cannot be provided
    /// (when the named key does not exist in the arguments IDictionary).</exception>
    /// <remarks>
    /// <para>
    /// Note that argument names may or may not be case-sensitive depending on the
    /// comparer used by the dictionary. To get case-insensitive behaviour, use
    /// a dictionary that has a case-insensitive comparer.
    /// </para>
    /// <para>
    /// For implementations where a user-supplied template is used to format 
    /// arguments provided by the system it is recommended that arguments
    /// are case-insensitive.
    /// </para>
    /// </remarks>
    public static string Format(string template, IDictionary<string, object> arguments) {
        return Format(null, template, arguments);
    }

    /// <summary>
    /// Replaces named template items in a specified string 
    /// with the string representation of a corresponding named object 
    /// provided by the passed function. 
    /// A specified parameter supplies culture-specific formatting information.
    /// </summary>
    /// <param name="template">A template string (see Remarks).</param>
    /// <param name="getValue">An function that supplies named objects to format.</param>
    /// <returns>A copy of template in which the template items have been replaced 
    /// by the string representation of the corresponding objects supplied by getValue.</returns>
    /// <exception cref="ArgumentNullException">template or the getValue callback is null.</exception>
    /// <exception cref="FormatException">template is invalid, 
    /// or one of the named template items cannot be provided
    /// (getValue returns false when that item is requested).</exception>
    /// <remarks>
    /// <para>
    /// Note that argument names may or may not be case-sensitive depending on the
    /// comparer used by the dictionary. To get case-insensitive behaviour, use
    /// a dictionary that has a case-insensitive comparer.
    /// </para>
    /// <para>
    /// For implementations where a user-supplied template is used to format 
    /// arguments provided by the system it is recommended that arguments
    /// are case-insensitive.
    /// </para>
    /// </remarks>
    public static string Format(string template, GetValue getValue) {
        return Format(null, template, getValue);
    }

    /// <summary>
    /// Replaces named template items in a specified string 
    /// with the string representation of a corresponding named object 
    /// from the specified dictionary. 
    /// A specified parameter supplies culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="template">A template string (see Remarks).</param>
    /// <param name="arguments">A dictionary that contains named objects to format.</param>
    /// <returns>A copy of template in which the template items have been replaced 
    /// by the string representation of the corresponding objects from arguments.</returns>
    /// <exception cref="ArgumentNullException">template or arguments is null.</exception>
    /// <exception cref="FormatException">template is invalid, 
    /// or one of the named template items cannot be provided
    /// (when the named key does not exist in the arguments IDictionary).</exception>
    /// <remarks>
    /// <para>
    /// Note that argument names may or may not be case-sensitive depending on the
    /// comparer used by the dictionary. To get case-insensitive behaviour, use
    /// a dictionary that has a case-insensitive comparer.
    /// </para>
    /// <para>
    /// For implementations where a user-supplied template is used to format 
    /// arguments provided by the system it is recommended that arguments
    /// are case-insensitive.
    /// </para>
    /// </remarks>
    public static string Format(IFormatProvider provider, string template, IDictionary<string, object> arguments) {
        if (arguments == null) {
            throw new ArgumentNullException(nameof(arguments));
        }

        return Format(provider, template, arguments.TryGetValue);
    }

    /// <summary>
    /// Replaces named template items in a specified string 
    /// with the string representation of a corresponding named object 
    /// provided by the passed function. 
    /// A specified parameter supplies culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="template">A template string (see Remarks).</param>
    /// <param name="getValue">An function that supplies named objects to format.</param>
    /// <returns>A copy of template in which the template items have been replaced 
    /// by the string representation of the corresponding objects supplied by getValue.</returns>
    /// <exception cref="ArgumentNullException">template or the getValue callback is null.</exception>
    /// <exception cref="FormatException">template is invalid, 
    /// or one of the named template items cannot be provided
    /// (getValue returns false when that item is requested).</exception>
    /// <remarks>
    /// <para>
    /// Note that argument names may or may not be case-sensitive depending on the
    /// comparer used by the dictionary. To get case-insensitive behaviour, use
    /// a dictionary that has a case-insensitive comparer.
    /// </para>
    /// <para>
    /// For implementations where a user-supplied template is used to format 
    /// arguments provided by the system it is recommended that arguments
    /// are case-insensitive.
    /// </para>
    /// </remarks>
    public static string Format(IFormatProvider provider, string template, GetValue getValue) {
        if (template == null) {
            throw new ArgumentNullException(nameof(template));
        }

        if (getValue == null) {
            throw new ArgumentNullException(nameof(getValue));
        }

        char[] chArray = template.ToCharArray(0, template.Length);
        int index = 0;
        int length = chArray.Length;

        ICustomFormatter formatter = null;
        if (provider != null) {
            formatter = (ICustomFormatter)provider.GetFormat(typeof(ICustomFormatter));
        }

        StringBuilder builder = new StringBuilder();
        while (index < length) {
            var ch = chArray[index];
            index++;
            if (ch == '}') {
                if ((index < length) && (chArray[index] == '}')) {
                    // Literal close curly brace
                    builder.Append('}');
                    index++;
                }
                else {
                    throw new FormatException("Input string was not a correct template");
                }
            }
            else if (ch == '{') {
                if ((index < length) && (chArray[index] == '{')) {
                    // Literal open curly brace
                    builder.Append('{');
                    index++;
                }
                else {
                    // Template item:
                    if (index == length) {
                        throw new FormatException("Input string was not a correct template");
                    }

                    // Argument name
                    int nameStart = index;
                    ch = chArray[index];
                    index++;
                    if (!(ch == '_'
                          || ch == '@'
                          || ch is >= 'a' and <= 'z'
                          || ch is >= 'A' and <= 'Z')) {
                        throw new FormatException("Input string was not a correct template");
                    }

                    while ((index < length) &&
                           (ch == '.' || ch == '-' || ch == '_' || ch == '@'
                            || ch is >= '0' and <= '9'
                            || ch is >= 'a' and <= 'z'
                            || ch is >= 'A' and <= 'Z')) {
                        ch = chArray[index];
                        index++;
                    }

                    int nameEnd = index - 1;
                    if (nameEnd == nameStart) {
                        throw new FormatException("Input string was not a correct template");
                    }

                    string argumentName = new string(chArray, nameStart, nameEnd - nameStart);
                    if (!getValue(argumentName, out var arg)) {
                        throw new FormatException("Input string was not a correct template");
                    }

                    // Skip blanks
                    while ((index < length) && (ch == ' ')) {
                        ch = chArray[index];
                        index++;
                    }

                    // Argument alignment
                    int width = 0;
                    bool leftAlign = false;
                    if (ch == ',') {
                        if (index == length) {
                            throw new FormatException("Input string was not a correct template");
                        }

                        ch = chArray[index];
                        index++;
                        while ((index < length) && (ch == ' ')) {
                            ch = chArray[index];
                            index++;
                        }

                        if (index == length) {
                            throw new FormatException("Input string was not a correct template");
                        }

                        if (ch == '-') {
                            leftAlign = true;
                            if (index == length) {
                                throw new FormatException("Input string was not a correct template");
                            }

                            ch = chArray[index];
                            index++;
                        }

                        if ((ch < '0') || (ch > '9')) {
                            throw new FormatException("Input string was not a correct template");
                        }

                        while ((index < length) && ch is >= '0' and <= '9') {
                            // TODO: What if number too large for Int32, i.e. throw exception
                            width = width * 10 + (ch - 0x30);
                            ch = chArray[index];
                            index++;
                        }
                    }

                    // Skip blanks
                    while ((index < length) && (ch == ' ')) {
                        ch = chArray[index];
                        index++;
                    }

                    // Format string
                    string formatString = null;
                    if (ch == ':') {
                        if (index == length) {
                            throw new FormatException("Input string was not a correct template");
                        }

                        int formatStart = index;
                        ch = chArray[index];
                        index++;
                        while ((index < length) && (ch != '{') && (ch != '}')) {
                            ch = chArray[index];
                            index++;
                        }

                        int formatEnd = index - 1;
                        if (formatEnd >= formatStart) {
                            formatString = new string(chArray, formatStart, formatEnd - formatStart);
                        }
                    }

                    // Insert formatted argument
                    if (ch != '}') {
                        throw new FormatException("Input string was not a correct template");
                    }

                    string argumentValue = null;
                    if (formatter != null) {
                        argumentValue = formatter.Format(formatString, arg, provider);
                    }

                    if (argumentValue == null) {
                        if (arg is IFormattable formattable) {
                            argumentValue = formattable.ToString(formatString, provider);
                        }
                        else if (arg != null) {
                            argumentValue = arg.ToString();
                        }
                    }

                    argumentValue ??= string.Empty;

                    int paddingCount = width - argumentValue.Length;
                    if (!leftAlign && (paddingCount > 0)) {
                        builder.Append(' ', paddingCount);
                    }

                    builder.Append(argumentValue);
                    if (leftAlign && (paddingCount > 0)) {
                        builder.Append(' ', paddingCount);
                    }
                }
            }
            else {
                // Literal -- scan up until next curly brace
                int literalStart = index - 1;
                while (index < length) {
                    ch = chArray[index];
                    if (ch == '{' || ch == '}') {
                        break;
                    }

                    index++;
                }

                builder.Append(chArray, literalStart, index - literalStart);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Provides named argument values for the StringTemplate.
    /// </summary>
    /// <param name="name">Name of the argument required.</param>
    /// <param name="value">Value of the argument, if it exists.</param>
    /// <returns>true if the argument name is valid, i.e. the value can be supplied; false if the argument name is invalid (usually treated as an error)</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public delegate bool GetValue(string name, out object value);
}