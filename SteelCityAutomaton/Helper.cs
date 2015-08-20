using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelCityAutomaton
{
    public static class Helper
    {
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool isPowerOfTwo(uint n)
        {
            return (n & (n - 1)) == 0 && n != 0;
        }

        public static int nearestPowerofTwo(int n,int limit = 1024)
        {
            int b = (int)Math.Round(Math.Log(n,2));
            b = (int)Math.Pow(2, b);
            return (b > limit? limit:b);
        }

        public static Int32 unixTimeStamp()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length = -1)
        {
            if(length < 1)length = data.Length - index;
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
