using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DynamicMojo
{
    class Program
    {
        static void Main(string[] args)
        {
            Animal kitty = new HouseCat();
            Animal lion = new Lion();
            var meow = typeof(HouseCat).GetMethod("Meow", BindingFlags.Instance | BindingFlags.NonPublic);
            var roar = typeof(Lion).GetMethod("Roar", BindingFlags.Instance | BindingFlags.NonPublic);

            Console.WriteLine("<==(Normal Run)==>");
            kitty.MakeNoise(); //HouseCat: Meow.
            lion.MakeNoise(); //Lion: Roar!

            Console.WriteLine("<==(Dynamic Mojo!)==>");
            DynamicMojo.SwapMethodBodies(meow, roar);
            kitty.MakeNoise(); //HouseCat: Roar!
            lion.MakeNoise(); //Lion: Meow.

            Console.WriteLine("<==(Normality Restored)==>");
            DynamicMojo.SwapMethodBodies(meow, roar);
            kitty.MakeNoise(); //HouseCat: Meow.
            lion.MakeNoise(); //Lion: Roar!

            Console.Read();
        }
    }

    public abstract class Animal
    {
        public void MakeNoise() => Console.WriteLine($"{this.GetType().Name}: {GetSound()}");

        protected abstract string GetSound();
    }

    public sealed class HouseCat : Animal
    {
        protected override string GetSound() => Meow();

        private string Meow() => "Meow.";
    }

    public sealed class Lion : Animal
    {
        protected override string GetSound() => Roar();

        private string Roar() => "Roar!";
    }

    public static class DynamicMojo
    {
        /// <summary>
        /// Swaps the function pointers for a and b, effectively swapping the method bodies.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// a and b must have same signature
        /// </exception>
        /// <param name="a">Method to swap</param>
        /// <param name="b">Method to swap</param>
        public static void SwapMethodBodies(MethodInfo a, MethodInfo b)
        {
            if (!HasSameSignature(a, b))
            {
                throw new ArgumentException("a and b must have have same signature");
            }

            RuntimeHelpers.PrepareMethod(a.MethodHandle);
            RuntimeHelpers.PrepareMethod(b.MethodHandle);

            unsafe
            {
                if (IntPtr.Size == 4)
                {
                    int* inj = (int*)b.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)a.MethodHandle.Value.ToPointer() + 2;

                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;

                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    int tmp = *tarSrc;
                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
                    *injSrc = (((int)tarInst + 5) + tmp) - ((int)injInst + 5);
                }
                else
                {
                    throw new NotImplementedException($"{nameof(SwapMethodBodies)} doesn't yet handle IntPtr size of {IntPtr.Size}");
                }
            }
        }

        private static bool HasSameSignature(MethodInfo a, MethodInfo b)
        {
            bool sameParams = !a.GetParameters().Any(x => !b.GetParameters().Any(y => x == y));
            bool sameReturnType = a.ReturnType == b.ReturnType;
            return sameParams && sameReturnType;
        }
    }
}