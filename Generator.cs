using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;

namespace Lab2
{
    static class Generator
    {
        static Random rand = new Random();
        public static double Exp(double l)
        {
            double r = rand.NextDouble();
            while(r == 0.0) r = rand.NextDouble();
            r = -l * Math.Log(r);
            return r;
        }

        public static double Unif(double a, double b)
        {
            double r = rand.NextDouble();
            while (r == 0.0) r = rand.NextDouble();
            r = a + r * (b - a);
            return r;
        }

        public static double Norm(double a, double s)
        {
            return a + s * StrangeSum();
        }

        public static double Erlang(double B, uint m)
        {
            double r = rand.NextDouble();
            return (Math.Pow(r, m - 1) * Math.Exp(r / B)) / (Math.Pow(B, m) * SpecialFunctions.Gamma(m));
        }

        private static double StrangeSum()
        {
            double sum = 0;
            for(int i = 0; i < 12; i++)
            {
                double r = rand.NextDouble();
                while (r == 0.0) r = rand.NextDouble();
                sum += r;
            }
            return sum - 6;
        }
    }
}
