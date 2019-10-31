using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reflection{
    public partial class ReflectionExtensions{
        /// <summary>
        /// Swaps the function pointers for a and b, effectively swapping the method bodies.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// a and b must have same signature
        /// </exception>
        /// <param name="a">Method to swap</param>
        /// <param name="b">Method to swap</param>
        public static void SwapMethodBodies(this MethodInfo a, MethodInfo b){
            if (!HasSameSignature(a, b)){
                throw new ArgumentException("a and b must have have same signature");
            }

            RuntimeHelpers.PrepareMethod(a.MethodHandle);
            RuntimeHelpers.PrepareMethod(b.MethodHandle);

            unsafe{
                if (IntPtr.Size == 4){
                    int* inj = (int*) b.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*) a.MethodHandle.Value.ToPointer() + 2;

                    byte* injInst = (byte*) *inj;
                    byte* tarInst = (byte*) *tar;

                    int* injSrc = (int*) (injInst + 1);
                    int* tarSrc = (int*) (tarInst + 1);

                    int tmp = *tarSrc;
                    *tarSrc = (((int) injInst + 5) + *injSrc) - ((int) tarInst + 5);
                    *injSrc = (((int) tarInst + 5) + tmp) - ((int) injInst + 5);
                }
                else{
                    throw new NotImplementedException(
                        $"{nameof(SwapMethodBodies)} doesn't yet handle IntPtr size of {IntPtr.Size}");
                }
            }
        }
    }
}