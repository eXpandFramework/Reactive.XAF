using System;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsNumeric(this Type type,bool decimals=false) {
            if (IsNumericCore(type)) {
                if (decimals) {
                    switch (Type.GetTypeCode(type)) {
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Single:
                            return true;
                        default:
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static bool IsNumericCore(Type type) {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}