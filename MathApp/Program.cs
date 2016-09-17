using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using System.Numerics;
using MathNet.Numerics;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.Expression;
using Eval = MathNet.Symbolics.Evaluate;
using System.Text.RegularExpressions;

namespace mathapp
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MathApp());
        }
    }

    class Util
    {
        //将字符串形式的数字转为Expr形式
        public static Expr parseNum(string str)
        {
            int index = 0;
            str = str + '$';
            long token_val = 0;

            if (str[index] == '-')
            {
                ++index;
                Expr temp = parseNum(str.Substring(index));
                if (temp == Expr.Undefined)
                    return Expr.Undefined;
                else
                    return Expr.Zero - temp;
            }
            else if (str[index] >= '0' && str[index] <= '9')
            {
                token_val = str[index] - '0';
                if (token_val > 0) //数字第一位大于0
                {
                    ++index;
                    while (str[index] >= '0' && str[index] <= '9') //数字是整数
                    {
                        token_val = token_val * 10 + str[index] - '0';
                        ++index;
                    }
                    if (str[index] == '.') //是大于1的小数
                    {
                        ++index;
                        long numerator = 0;
                        long denominator = 1;
                        while (str[index] >= '0' && str[index] <= '9')
                        {
                            numerator = numerator * 10 + str[index] - '0';
                            denominator = denominator * 10;
                            ++index;
                        }
                        return Rational.Rationalize(Expr.FromRational(BigRational.FromBigIntFraction(token_val * denominator + numerator, denominator)));
                    }
                    else //不是小数
                        return Rational.Rationalize(Expr.FromRational(BigRational.FromBigIntFraction(token_val, 1)));
                }
                else //数字第一位是0
                {
                    ++index;
                    if (str[index] == '.') //0后面是小数点
                    {
                        ++index;
                        long numerator = 0;
                        long denominator = 1;
                        while (str[index] >= '0' && str[index] <= '9')
                        {
                            numerator = numerator * 10 + str[index] - '0';
                            denominator = denominator * 10;
                            ++index;
                        }
                        return Rational.Rationalize(Expr.FromRational(BigRational.FromBigIntFraction(numerator, denominator)));
                    }
                    else if (str[index] == 'x' || str[index] == 'X') //0后面是x或者X：十六进制数
                    {
                        ++index;
                        while ((str[index] >= '0' && str[index] <= '9') || (str[index] >= 'a' && str[index] <= 'f') || (str[index] >= 'A' && str[index] <= 'F'))
                        {
                            token_val = token_val * 16 + (str[index] & 15) + (str[index] >= 'A' ? 9 : 0);
                            ++index;
                        }
                        return Rational.Rationalize(Expr.FromRational(BigRational.FromBigIntFraction(token_val, 1)));
                    }
                    else //0后面不是x，X或 . ：八进制数
                    {
                        while (str[index] >= '0' && str[index] <= '7')
                        {
                            token_val = token_val * 8 + str[index] - '0';
                            ++index;
                        }
                        return Rational.Rationalize(Expr.FromRational(BigRational.FromBigIntFraction(token_val, 1)));
                    }
                }
            }
            else
                return Expr.Undefined;
        }

        //统计一个Expr的项数
        public static int numberOfTerm(Expr input)
        {
            int n = 0;

            if (input == Expr.Undefined)
                return n;

            string temp_s = Infix.Format(input);
            temp_s = temp_s + "$$";
            int index = 0;
            int br_num = 0;
            bool condition = true;
            while (index < temp_s.Length - 1 || temp_s[index] != '$')
            {
                if (temp_s[index] == '-' && index == 0)
                {
                    ++index;
                    while (condition && temp_s[index] != '$')
                    {
                        if (temp_s[index] == '(')
                            ++br_num;
                        else if (temp_s[index] == ')')
                            --br_num;

                        if (index == temp_s.Length)
                            condition = false;
                        else if (br_num > 0 && (temp_s[index] == '+' || temp_s[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (temp_s[index] == '+' || temp_s[index] == '-'))
                            condition = false;

                        ++index;
                    }
                    ++n;
                    ++index;
                }
                else if (temp_s[index] == ' ')
                {
                    ++index;
                }
                else
                {
                    br_num = 0;
                    condition = true;
                    while (condition && temp_s[index] != '$')
                    {
                        if (temp_s[index] == '(')
                            ++br_num;
                        else if (temp_s[index] == ')')
                            --br_num;

                        if (index == temp_s.Length)
                            condition = false;
                        else if (br_num > 0 && (temp_s[index] == '+' || temp_s[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (temp_s[index] == '+' || temp_s[index] == '-'))
                            condition = false;

                        ++index;
                    }
                    ++n;
                    ++index;
                }
            }

            return n;
        }
        //将一个Expr按+，-拆分为多个单项Expr
        public static Queue<Expr> extractTermFromExpr(Expr input)
        {
            Queue<Expr> queue = new Queue<Expr>();
            HashSet<int> op = new HashSet<int>() { '*', '/', '^' };

            if (input == Expr.Undefined)
                return queue;

            string temp = Infix.Format(input);
            temp = temp + '$';
            int index = 0;
            int br_num = 0;
            bool condition = true;
            Expr temp_e = Expr.Zero;

            while (index < temp.Length && temp[index] != '$')
            {
                if (temp[index] == ' ')
                {
                    temp = temp.Remove(index, 1);
                    ++index;
                }
                else if (temp[index].ToString() == Infix.Format(Expr.Pi))
                {
                    temp = temp.Remove(index, 1);
                    temp = temp.Insert(index, "PI");
                    index += 2;
                }
                else if (temp[index] == 'j')
                {
                    temp = temp.Remove(index, 1);
                    temp = temp.Insert(index, "i");
                    ++index;
                }
                else
                    ++index;
            }

            index = 0;
            while (index < temp.Length && temp[index] != '$')
            {
                string temp_s = "";

                if (temp[index] == '-')
                {
                    ++index;
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    temp_e = Member.parse(temp_s);
                    queue.Enqueue(Expr.Zero - temp_e);
                }
                else if (temp[index] == '+')
                {
                    ++index;
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    temp_e = Member.parse(temp_s);
                    queue.Enqueue(temp_e);
                }
                else
                {
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    temp_e = Member.parse(temp_s);
                    queue.Enqueue(temp_e);
                }
            }
            return queue;
        }
        //函数重载，处理string形式的expresion
        public static Queue<string> extractTermFromExpr(string input)
        {
            Queue<string> queue = new Queue<string>();
            HashSet<int> op = new HashSet<int>() { '*', '/', '^' };

            if (input == "")
                return queue;

            string temp = input;
            temp = temp + '$';
            int index = 0;
            int br_num = 0;
            bool condition = true;

            while (index < temp.Length && temp[index] != '$')
            {
                if (temp[index] == ' ')
                {
                    temp = temp.Remove(index, 1);
                    ++index;
                }
                else
                    ++index;
            }

            index = 0;
            while (index < temp.Length && temp[index] != '$')
            {
                string temp_s = "";

                if (temp[index] == '-')
                {
                    temp_s += temp[index];
                    ++index;
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    queue.Enqueue(temp_s);
                }
                else if (temp[index] == '+')
                {
                    ++index;
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    queue.Enqueue(temp_s);
                }
                else
                {
                    condition = true;
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_s += temp[index];
                        ++index;

                        if (temp[index] == '$')
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = true;
                        else if (br_num == 0 && (((temp[index] == '-' && op.Contains(temp[index - 1])) || (temp[index] == '+') && op.Contains(temp[index - 1]))))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '+' || temp[index] == '-'))
                            condition = false;
                    }
                    queue.Enqueue(temp_s);
                }
            }
            return queue;
        }
        //在Expr中找出所有的symbol
        public static HashSet<Expr> collectSymbols(Expr input)
        {
            HashSet<Expr> output = new HashSet<Expr>();
            Queue<Expr> queue = extractTermFromExpr(input);

            while (queue.Count > 0)
            {
                HashSet<Expr> set = collectSymbol(queue.Dequeue());
                if (set != null)
                {
                    foreach (Expr e1 in set)
                        output.Add(e1);
                }
            }
            return output;
        }

        private static HashSet<Expr> collectSymbol(Expr input)
        {
            HashSet<Expr> output = new HashSet<Expr>();
            string temp = Infix.Format(input);

            bool b = input.IsFunction || input.IsPower || input.IsProduct;
            if (temp[0] == '-')
            {
                temp = temp.Substring(1);
                Expr temp_in = Member.parse(temp);
                return collectSymbol(temp_in);
            }
            if (!(b || input.IsNumber)) // 表达式为自定义函数
            {
                string temp_s = Infix.Format(input);
                int ind = temp_s.IndexOf('(');
                if (ind == -1)
                    output.Add(input);
                else
                {
                    output.Add(input);
                    Expr temp_expr = Member.parse(temp_s.Remove(temp_s.Length - 1, 1).Substring(ind + 1));
                    HashSet<Expr> temp_set = collectSymbols(temp_expr);
                    if (temp_set != null)
                    {
                        foreach (Expr e in temp_set)
                            output.Add(e);
                    }
                }
                return output;
            }
            else if (input.IsConstant || input.IsNumber)
            {
                return null;
            }
            else if (input.IsFunction) // 函数
            {
                string temp_s = Infix.Format(input);
                int ind = temp_s.IndexOf('(');
                Expr temp_expr = Member.parse(temp_s.Remove(temp_s.Length - 1, 1).Substring(ind + 1));
                HashSet<Expr> temp_set = collectSymbols(temp_expr);
                if (temp_set != null)
                {
                    foreach (Expr e in temp_set)
                        output.Add(e);
                }
                return output;
            }
            else if (input.IsProduct) // 函数乘积
            {
                string temp_s = Infix.Format(input);
                int index = 0;
                int br_num = 0;
                bool condition = true;
                string temp_1 = "", temp_2 = "";
                while (condition)
                {
                    if (temp_s[index] == '(')
                        ++br_num;
                    else if (temp_s[index] == ')')
                        --br_num;

                    temp_1 += temp_s[index];
                    ++index;

                    if (index == temp_s.Length)
                        condition = false;
                    else if (br_num > 0 && (temp_s[index] == '*' || temp_s[index] == '/'))
                        condition = true;
                    else if (br_num == 0 && (temp_s[index] == '*' || temp_s[index] == '/'))
                        condition = false;
                }
                temp_2 = temp_s.Substring(index + 1);
                Expr temp_expr1 = Member.parse(temp_1);
                HashSet<Expr> temp_set1 = collectSymbols(temp_expr1);
                if (temp_set1 != null)
                {
                    foreach (Expr e in temp_set1)
                        output.Add(e);
                }
                Expr temp_expr2 = Member.parse(temp_2);
                HashSet<Expr> temp_set2 = collectSymbols(temp_expr2);
                if (temp_set2 != null)
                {
                    foreach (Expr e in temp_set2)
                        output.Add(e);
                }
                return output;
            }
            else // 函数乘方
            {
                string temp_s = Infix.Format(input);
                int index = 0;
                int br_num = 0;
                bool condition = true;
                string temp_1 = "", temp_2 = "";
                while (condition)
                {
                    if (temp_s[index] == '(')
                        ++br_num;
                    else if (temp_s[index] == ')')
                        --br_num;

                    temp_1 += temp_s[index];
                    ++index;

                    if (index == temp_s.Length)
                        condition = false;
                    else if (br_num > 0 && temp_s[index] == '^')
                        condition = true;
                    else if (br_num == 0 && temp_s[index] == '^')
                        condition = false;
                }

                if (index < temp_s.Length)
                    temp_2 = temp_s.Substring(index + 1);
                else
                    temp_2 = "";

                Expr temp_expr1 = Member.parse(temp_1);
                if (temp_expr1.IsPower)
                {
                    index = 0;
                    br_num = 0;
                    condition = true;
                    string temp_11 = "", temp_22 = "";
                    while (condition)
                    {
                        if (temp_1[index] == '(')
                            ++br_num;
                        else if (temp_1[index] == ')')
                            --br_num;

                        temp_11 += temp_1[index];
                        ++index;

                        if (index == temp_1.Length)
                            condition = false;
                        else if (br_num > 0 && temp_1[index] == '/')
                            condition = true;
                        else if (br_num == 0 && temp_1[index] == '/')
                            condition = false;
                    }
                    if (index == temp_1.Length)
                        temp_22 = "";
                    else
                        temp_22 = temp_1.Substring(index + 1);

                    Expr temp_expr11 = Member.parse(temp_11);
                    HashSet<Expr> temp_set11 = collectSymbols(temp_expr11);
                    if (temp_set11 != null)
                    {
                        foreach (Expr e in temp_set11)
                            output.Add(e);
                    }
                    Expr temp_expr22 = Member.parse(temp_22);
                    HashSet<Expr> temp_set22 = collectSymbols(temp_expr22);
                    if (temp_set22 != null)
                    {
                        foreach (Expr e in temp_set22)
                            output.Add(e);
                    }

                    if (temp_2 != "")
                    {
                        Expr temp_expr2 = Member.parse(temp_2);
                        HashSet<Expr> temp_set2 = collectSymbols(temp_expr2);
                        if (temp_set2 != null)
                        {
                            foreach (Expr e in temp_set2)
                                output.Add(e);
                        }
                    }
                }
                else if (temp_2 != "")
                {
                    HashSet<Expr> temp_set1 = collectSymbols(temp_expr1);
                    if (temp_set1 != null)
                    {
                        foreach (Expr e in temp_set1)
                            output.Add(e);
                    }
                    Expr temp_expr2 = Member.parse(temp_2);
                    HashSet<Expr> temp_set2 = collectSymbols(temp_expr2);
                    if (temp_set2 != null)
                    {
                        foreach (Expr e in temp_set2)
                            output.Add(e);
                    }
                }
                else
                {
                    HashSet<Expr> temp_set1 = collectSymbols(temp_expr1);
                    if (temp_set1 != null)
                    {
                        foreach (Expr e in temp_set1)
                            output.Add(e);
                    }
                }
                return output;
            }
        }
        //解一元二次方程
        private static Complex[] solve_2_eq(Expr arg, Expr input)
        {
            Complex[] output = new Complex[2];

            Expr[] factors = Polynomial.Coefficients(arg, input);
            Expr A = factors[0];
            Expr B = factors[1];
            Expr C = factors[2];
            Expr DELTA = Expr.Pow(B, 2) - 4 * A * C;
            FloatingPoint a = Eval.Evaluate(null, A).RealValue;
            FloatingPoint b = Eval.Evaluate(null, B).RealValue;
            FloatingPoint c = Eval.Evaluate(null, C).RealValue;

            FloatingPoint delta = Eval.Evaluate(null, Expr.Root(2, DELTA));

            if (delta.IsReal)
            {
                output[0] = Eval.Evaluate(null, (-B + Expr.Root(2, DELTA)) / (2 * A)).ComplexValue;
                output[1] = Eval.Evaluate(null, (-B - Expr.Root(2, DELTA)) / (2 * A)).ComplexValue;
            }
            else
            {
                output[0] = Eval.Evaluate(null, (-B + Expr.Root(2, Expr.Abs(DELTA)) * Expr.I) / (2 * A)).ComplexValue;
                output[1] = Eval.Evaluate(null, (-B - Expr.Root(2, Expr.Abs(DELTA)) * Expr.I) / (2 * A)).ComplexValue;
            }

            return output;
        }
        //解一元三次方程
        private static Complex[] solve_3_eq(Expr arg, Expr input)
        {
            Complex[] output = new Complex[3];

            Expr[] factors = Polynomial.Coefficients(arg, input);
            Expr d = factors[0];
            Expr c = factors[1];
            Expr b = factors[2];
            Expr a = factors[3];

            if (Numbers.Compare(b, 0) == 0 && Numbers.Compare(c, 0) == 0 && Numbers.Compare(a * d, 0) == -1)
                output[0] = output[1] = output[2] = complex_handle(Eval.Evaluate(null, Expr.Root(3, Expr.Abs(d / a))).ComplexValue);
            else
            {
                Expr DELTA = 18 * a * b * c * d - 4 * Expr.Pow(b, 3) * d + Expr.Pow(b, 2) * Expr.Pow(c, 2) - 4 * a * Expr.Pow(c, 3) - 27 * Expr.Pow(a, 2) * Expr.Pow(d, 2);
                Expr DELTA_0 = Expr.Pow(b, 2) - 3 * a * c;
                Expr DELTA_1 = 2 * Expr.Pow(b, 3) - 9 * a * b * c + 27 * Expr.Pow(a, 2) * d;
                Expr DELTA_2;

                if (Eval.Evaluate(null, -27 * Expr.Pow(a, 2) * DELTA).RealValue >= 0)
                    DELTA_2 = Expr.Root(2, -27 * Expr.Pow(a, 2) * DELTA / 4);
                else
                    DELTA_2 = Expr.Root(2, 27 * Expr.Pow(a, 2) * DELTA / 4) * Expr.I;

                Expr temp = DELTA_1 / 2 + DELTA_2;
                Expr C;
                if (Eval.Evaluate(null, -27 * Expr.Pow(a, 2) * DELTA).RealValue >= 0)
                {
                    double t = Eval.Evaluate(null, temp).RealValue;
                    if (t > 0)
                        C = Expr.Root(3, temp);
                    else
                        C = -Expr.Root(3, Expr.Abs(temp));
                }
                else
                    C = Expr.Root(3, temp);

                double delta_0 = Eval.Evaluate(null, DELTA_0).RealValue;
                double delta_1 = Eval.Evaluate(null, DELTA_1).RealValue;
                Complex delta_2 = complex_handle(Eval.Evaluate(null, DELTA_2).ComplexValue);
                Complex cc = complex_handle(Eval.Evaluate(null, C).ComplexValue);
                double real = -0.5;
                double img = Eval.Evaluate(null, Expr.Root(2, 3)).RealValue / 2;
                Complex k = new Complex(real, img);

                double frac = Eval.Evaluate(null, -(1 / (3 * a))).RealValue;
                double bb = Eval.Evaluate(null, b).RealValue;

                output[0] = complex_handle(frac * (bb + cc + delta_0 / cc));
                output[1] = complex_handle(frac * (bb + k * cc + delta_0 / (k * cc)));
                output[2] = complex_handle(frac * (bb + Complex.Pow(k, 2) * cc + delta_0 / (Complex.Pow(k, 2) * cc)));
            }

            return output;
        }
        //解一元四次方程
        private static Complex[] solve_4_eq(Expr arg, Expr input)
        {
            Complex[] output = new Complex[4];

            Expr[] factors = Polynomial.Coefficients(arg, input);
            Expr e = factors[0];
            Expr d = factors[1];
            Expr c = factors[2];
            Expr b = factors[3];
            Expr a = factors[4];

            if (Numbers.Compare(b, 0) == 0 && Numbers.Compare(c, 0) == 0 && Numbers.Compare(d, 0) == 0 && Numbers.Compare(a * e, 0) == -1)
                output[0] = output[1] = output[2] = output[3] = complex_handle(Eval.Evaluate(null, Expr.Root(4, Expr.Abs(e / a))).ComplexValue);
            else
            {
                Expr DELTA = 256 * Expr.Pow(a, 3) * Expr.Pow(e, 3) - 192 * Expr.Pow(a, 2) * b * d * Expr.Pow(e, 2) - 128 * Expr.Pow(a, 2) * Expr.Pow(c, 2) * Expr.Pow(e, 2) + 144 * Expr.Pow(a, 2) * c * Expr.Pow(d, 2) * e - 27 * Expr.Pow(a, 2) * Expr.Pow(d, 4)
                                    + 144 * a * Expr.Pow(b, 2) * c * Expr.Pow(e, 2) - 6 * a * Expr.Pow(b, 2) * Expr.Pow(d, 2) * e - 80 * a * b * Expr.Pow(c, 2) * d * e + 18 * a * b * c * Expr.Pow(d, 3) + 16 * a * Expr.Pow(c, 4) * e - 4 * a * Expr.Pow(c, 3) * Expr.Pow(d, 2)
                                    - 27 * Expr.Pow(b, 4) * Expr.Pow(e, 2) + 18 * Expr.Pow(b, 3) * c * d * e - 4 * Expr.Pow(b, 3) * Expr.Pow(d, 3) - 4 * Expr.Pow(b, 2) * Expr.Pow(c, 3) * e + Expr.Pow(b, 2) * Expr.Pow(c, 2) * Expr.Pow(d, 2);
                Expr DELTA_0 = Expr.Pow(c, 2) - 3 * b * d + 12 * a * e;
                Expr DELTA_1 = 2 * Expr.Pow(c, 2) - 9 * b * c * d + 27 * Expr.Pow(b, 2) * e + 27 * a * Expr.Pow(d, 2) - 72 * a * c * e;
                Expr DELTA_2;

                if (Eval.Evaluate(null, -27 * DELTA).RealValue >= 0)
                    DELTA_2 = Expr.Root(2, -27 * DELTA / 4);
                else
                    DELTA_2 = Expr.Root(2, Expr.Abs(-27 * DELTA / 4)) * Expr.I;

                Expr Q = Expr.Root(3, DELTA_1 / 2 + DELTA_2);
                Expr p = (8 * a * c - 3 * Expr.Pow(b, 2)) / (8 * Expr.Pow(a, 2));
                Expr q = (Expr.Pow(b, 3) - 4 * a * b * c + 8 * Expr.Pow(a, 2) * d) / (8 * Expr.Pow(a, 2));
                Expr S_frac = (-2 / 3) * p + (1 / (3 * a)) * (Q + DELTA_0 / Q);

                Complex s_f = complex_handle(Eval.Evaluate(null, S_frac).ComplexValue);
                Complex ss = complex_handle(0.5 * Complex.Sqrt(s_f));

                double frac = Eval.Evaluate(null, -b / (4 * a)).RealValue;
                Complex QQ = complex_handle(Eval.Evaluate(null, Q).ComplexValue);
                double pp = Eval.Evaluate(null, p).RealValue;
                double qq = Eval.Evaluate(null, q).RealValue;
                Complex frac_0 = complex_handle(Complex.Sqrt(-4 * Complex.Pow(ss, 2) - 2 * pp + qq / ss));
                Complex frac_1 = complex_handle(Complex.Sqrt(-4 * Complex.Pow(ss, 2) - 2 * pp - qq / ss));

                output[0] = complex_handle(frac - ss + 0.5 * frac_0);
                output[1] = complex_handle(frac - ss - 0.5 * frac_0);
                output[2] = complex_handle(frac + ss + 0.5 * frac_1);
                output[3] = complex_handle(frac + ss - 0.5 * frac_1);
            }

            return output;
        }
        //处理绝对值较小的数
        public static Complex complex_handle(Complex c)
        {
            Complex output;

            if (c.Real.Magnitude() > -14 && c.Imaginary.Magnitude() > -14)
                output = new Complex(c.Real, c.Imaginary);
            else if (c.Real.Magnitude() > -14 && c.Imaginary.Magnitude() < -14)
                output = new Complex(c.Real, 0);
            else if (c.Real.Magnitude() < -14 && c.Imaginary.Magnitude() > -14)
                output = new Complex(0, c.Imaginary);
            else
                output = new Complex(0, 0);

            return output;
        }

        public static Complex[] eqSolver(Expr arg, Expr input)
        {
            Expr[] factors = Polynomial.Coefficients(arg, input);
            int n = factors.Length - 1;
            Complex[] output = new Complex[n];

            if (n == 2)
                output = solve_2_eq(arg, input);
            else if (n == 3)
                output = solve_3_eq(arg, input);
            else if (n == 4)
                output = solve_4_eq(arg, input);
            else
                output = null;

            return output;
        }

    }

    public class Member
    {

        public static Expr expression;
        private static List<int> tokens = new List<int>();
        private static Queue<Expr> symbols = new Queue<Expr>();
        private static HashSet<int> ops = new HashSet<int>() { '*', '/', '^', ',', '(', '+', '-', ')' };
        private static int NUM = 128, VAR = 129, CONST = 130;
        private static double LOG_DBL_MIN = -7.0839641853226408e+02;

        /*
         *  parser : USE tokenizer &  recursive descent
         * */
        public static Expr parse(string str)
        {
            tokenizer(str);
            expression = expr(tokens);
            tokens.Clear();
            symbols.Clear();
            return expression;
        }

        /*
         * <factor> := (<expr>)
         *                 | num
         *                 | var
         *                 | -num
         *                 | -var
         *                 | var(expr)
         *                 | -var(expr)
         * */
        private static Expr factor(List<int> token)
        {
            if (token.First() == NUM || token.First() == CONST)  // num
            {
                token.RemoveAt(0);
                Expr val = symbols.Dequeue();
                return val;
            }
            else if (token.First() == '-') // -
            {
                token.RemoveAt(0);
                Expr val = Expr.Zero;
                if (token.First() == NUM || token.First() == CONST) // -num
                {
                    token.RemoveAt(0);
                    val = val - symbols.Dequeue();
                    return val;
                }
                else if (token.First() == VAR)
                {
                    Expr var = symbols.Dequeue();
                    token.RemoveAt(0);
                    if (token.Count == 0) // -var
                    {
                        val = val - var;
                        return val;
                    }
                    else if (token.First() == '(') // -var()
                    {
                        token.RemoveAt(0);
                        token.RemoveAt(token.LastIndexOf(')'));

                        // 解析函数
                        int br_num = 0;
                        int index = 0;
                        bool condition = true;
                        List<int> lval = new List<int>();
                        while (condition) // 解析log, pow 和 root
                        {
                            if (token.ElementAt(index) == '(')
                                ++br_num;
                            else if (token.ElementAt(index) == ')')
                                --br_num;

                            lval.Add(token.ElementAt(index));
                            ++index;

                            if (index == token.Count)
                                condition = false;
                            else if (br_num > 0 && token.ElementAt(index) == ',')
                                condition = true;
                            else if (br_num == 0 && token.ElementAt(index) == ',')
                                condition = false;
                        }

                        if (lval.Count == token.Count)
                            token.RemoveRange(0, index);
                        else if (lval.Count < token.Count)
                            token.RemoveRange(0, index + 1);

                        Expr func;
                        string f_name = Infix.Format(var);
                        if (token.Count > 0) // 函数为log, root, pow 或者自定义的双参数函数
                        {
                            Expr arg1 = factor(lval);
                            Expr arg2 = factor(token);
                            switch (f_name)
                            {
                                case "root": func = Expr.Root(arg1, arg2); break;
                                case "pow": func = Expr.Pow(arg1, arg2); break;
                                case "log": func = Expr.Log(arg1, arg2); break;
                                default: func = Expr.Symbol(f_name + "(" + Infix.Format(arg1) + "," + Infix.Format(arg2) + ")"); break;
                            }
                            return val - func;
                        }
                        else // 函数为单参数函数
                        {
                            Expr variable = expr(lval);
                            switch (f_name)
                            {
                                case "fac": func = Factorial(variable); break;
                                case "sqrt": func = Expr.Sqrt(variable); break;
                                case "abs": func = Expr.Abs(variable); break;
                                case "sin": func = Expr.Sin(variable); break;
                                case "cos": func = Expr.Cos(variable); break;
                                case "tan": func = Expr.Tan(variable); break;
                                case "asin": func = Expr.ArcSin(variable); break;
                                case "acos": func = Expr.ArcCos(variable); break;
                                case "atan": func = Expr.ArcTan(variable); break;
                                case "sinh": func = Expr.Sinh(variable); break;
                                case "cosh": func = Expr.Cosh(variable); break;
                                case "tanh": func = Expr.Tanh(variable); break;
                                case "ln": func = Expr.Ln(variable); break;
                                case "exp": func = Expr.Exp(variable); break;
                                case "inv": func = Expr.Invert(variable); break;
                                default: func = Expr.Symbol(f_name + "(" + Infix.Format(variable) + ")"); break;
                            }
                            return val - func;
                        }
                    }
                    else // -var
                    {
                        val = val - var;
                        return val;
                    }
                }
            }
            else if (token.First() == VAR)
            {
                Expr var = symbols.Dequeue();
                token.RemoveAt(0);
                if (token.Count == 0) // var
                {
                    return var;
                }
                else if (token.First() == '(') // var()
                {
                    token.RemoveAt(0);
                    token.RemoveAt(token.LastIndexOf(')'));

                    // 解析函数
                    int br_num = 0;
                    int index = 0;
                    bool condition = true;
                    List<int> lval = new List<int>();
                    while (condition) // 解析log, pow 和 root
                    {
                        if (token.ElementAt(index) == '(')
                            ++br_num;
                        else if (token.ElementAt(index) == ')')
                            --br_num;

                        lval.Add(token.ElementAt(index));
                        ++index;

                        if (index == token.Count)
                            condition = false;
                        else if (br_num > 0 && token.ElementAt(index) == ',')
                            condition = true;
                        else if (br_num == 0 && token.ElementAt(index) == ',')
                            condition = false;
                    }

                    if (lval.Count == token.Count)
                        token.RemoveRange(0, index);
                    else if (lval.Count < token.Count)
                        token.RemoveRange(0, index + 1);

                    Expr func;
                    string f_name = Infix.Format(var);
                    if (token.Count > 0)
                    {
                        Expr arg1 = factor(lval);
                        Expr arg2 = factor(token);
                        switch (f_name)
                        {
                            case "root": func = Expr.Root(arg1, arg2); break;
                            case "pow": func = Expr.Pow(arg1, arg2); break;
                            case "log": func = Expr.Log(arg1, arg2); break;
                            default: func = Expr.Symbol(f_name + "(" + Infix.Format(arg1) + "," + Infix.Format(arg2) + ")"); break;
                        }
                        return func;
                    }
                    else
                    {
                        Expr variable = expr(lval);
                        switch (f_name)
                        {
                            case "fac": func = Factorial(variable); break;
                            case "sqrt": func = Expr.Sqrt(variable); break;
                            case "abs": func = Expr.Abs(variable); break;
                            case "sin": func = Expr.Sin(variable); break;
                            case "cos": func = Expr.Cos(variable); break;
                            case "tan": func = Expr.Tan(variable); break;
                            case "asin": func = Expr.ArcSin(variable); break;
                            case "acos": func = Expr.ArcCos(variable); break;
                            case "atan": func = Expr.ArcTan(variable); break;
                            case "sinh": func = Expr.Sinh(variable); break;
                            case "cosh": func = Expr.Cosh(variable); break;
                            case "tanh": func = Expr.Tanh(variable); break;
                            case "ln": func = Expr.Ln(variable); break;
                            case "exp": func = Expr.Exp(variable); break;
                            case "inv": func = Expr.Invert(variable); break;
                            default: func = Expr.Symbol(f_name + "(" + Infix.Format(variable) + ")"); break;
                        }
                        return func;
                    }
                }
                else // var
                {
                    return var;
                }
            }
            else if (token.First() == '(')// ()
            {
                token.RemoveAt(0);
                token.RemoveAt(token.LastIndexOf(')'));
                Expr val = expr(token);
                return val;
            }
            return Expr.Zero;
        }

        private static Expr Factorial(Expr input)
        {
            if (!input.IsNumber)
                return Expr.Symbol(Infix.Format(input) + "!");
            if (Numbers.Compare(input, 0) == -1)
                return Expr.Symbol("(" + Infix.Format(input) + ")" + "!");

            Expr output = Expr.One;
            if (input == Expr.Zero)
                return output;
            else
            {
                while (Numbers.Compare(input, 0) > 0)
                {
                    output = input * output;
                    input = input - 1;
                }
                return output;
            }
        }

        /*
         * <expr> := <term> <expr_tail>
         * <expr_tail> := + <term> <expr_tail>
         *                    | - <term> <expr_tail>
         *                    | <empty>
         * */
        private static Expr expr(List<int> token)
        {
            HashSet<int> op = new HashSet<int>() { '*', '/', '^' };
            int br_num = 0;
            int index = 0;
            List<int> lval = new List<int>();
            if (token.Count == 0)
                return Expr.Undefined;
            else if (token.First() == '-') //第一个字符是 - 
            {
                lval.Add(token.ElementAt(index));
                ++index;
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    // 跳过括号中的符号
                    else if (br_num > 0 && (token.ElementAt(index) == '-' || token.ElementAt(index) == '+'))
                        condition = true;
                    // 如果 + 或者 - 之前有 * ， / 或者 ^ , 则跳过继续
                    else if (br_num == 0 && (((token.ElementAt(index) == '-' && op.Contains(token.ElementAt(index - 1))) || (token.ElementAt(index) == '+') && op.Contains(token.ElementAt(index - 1)))))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '-' || token.ElementAt(index) == '+'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = term_1(lval);
                return exprTail(lvalue, token);
            }
            else //第一个字符非 - 
            {
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '-' || token.ElementAt(index) == '+'))
                        condition = true;
                    else if (br_num == 0 && (((token.ElementAt(index) == '-' && op.Contains(token.ElementAt(index - 1))) || (token.ElementAt(index) == '+') && op.Contains(token.ElementAt(index - 1)))))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '-' || token.ElementAt(index) == '+'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = term_1(lval);
                return exprTail(lvalue, token);
            }
        }
        /*
         * <term> := <term2> <term_tail>
         * <term_tail> := * <term2> <term_tail>
         *                     | / <term2> <term_tail>
         *                     | <empty>
         * */
        private static Expr term_1(List<int> token)
        {
            int br_num = 0;
            int index = 0;
            List<int> lval = new List<int>();
            if (token.First() == '-')
            {
                lval.Add(token.ElementAt(index));
                ++index;
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = term_2(lval);
                return term_1_Tail(lvalue, token);
            }
            else
            {
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = term_2(lval);
                return term_1_Tail(lvalue, token);
            }
        }
        /*
        * <term2> := <factor> <term2_tail>
         * <term2_tail> := ^ <factor> <term2_tail>
        *                     | <empty>
        * */
        private static Expr term_2(List<int> token)
        {
            Expr val = Expr.Zero;
            int index = 0;
            int br_num = 0;
            List<int> lval = new List<int>();

            if (!token.Contains('^'))
            {
                val = factor(token);
                return val;
            }
            else if (token.First() == '-')
            {
                ++index;
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && token.ElementAt(index) == '^')
                        condition = true;
                    else if (br_num == 0 && token.ElementAt(index) == '^')
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = factor(lval);
                return val - term_2_Tail(lvalue, token);
            }
            else
            {
                bool condition = true;
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && token.ElementAt(index) == '^')
                        condition = true;
                    else if (br_num == 0 && token.ElementAt(index) == '^')
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr lvalue = factor(lval);
                return term_2_Tail(lvalue, token);
            }
        }

        private static Expr term_2_Tail(Expr lvalue, List<int> token)
        {
            int br_num = 0;
            int index = 0;
            Expr val = Expr.Zero;
            List<int> lval = new List<int>();

            if (index == token.Count)
            {
                return lvalue;
            }
            else if (token.First() == '^')
            {
                ++index;
                if (token.ElementAt(index) == '-')
                {
                    ++index;
                    bool condition = true;
                    while (condition)
                    {
                        if (token.ElementAt(index) == '(')
                            ++br_num;
                        else if (token.ElementAt(index) == ')')
                            --br_num;

                        lval.Add(token.ElementAt(index));
                        ++index;
                        if (index == token.Count)
                            condition = false;
                        else if (br_num > 0 && token.ElementAt(index) == '^')
                            condition = true;
                        else if (br_num == 0 && token.ElementAt(index) == '^')
                            condition = false;
                    }
                    token.RemoveRange(0, index);
                    Expr value = Expr.Pow(lvalue, Expr.Zero - factor(lval));
                    return term_2_Tail(value, token);
                }
                else
                {
                    bool condition = true;
                    while (condition)
                    {
                        if (token.ElementAt(index) == '(')
                            ++br_num;
                        else if (token.ElementAt(index) == ')')
                            --br_num;

                        lval.Add(token.ElementAt(index));
                        ++index;
                        if (index == token.Count)
                            condition = false;
                        else if (br_num > 0 && token.ElementAt(index) == '^')
                            condition = true;
                        else if (br_num == 0 && token.ElementAt(index) == '^')
                            condition = false;
                    }
                    token.RemoveRange(0, index);
                    Expr value = Expr.Pow(lvalue, factor(lval));
                    return term_2_Tail(value, token);
                }
            }
            else
            {
                return lvalue;
            }
        }

        private static Expr term_1_Tail(Expr lvalue, List<int> token)
        {
            int br_num = 0;
            int index = 0;
            bool condition = true;
            List<int> lval = new List<int>();
            if (index == token.Count)
            {
                return lvalue;
            }
            else if (token.ElementAt(0) == '*')
            {
                token.RemoveAt(0);
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;
                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr value = lvalue * term_2(lval);
                return term_1_Tail(value, token);
            }
            else if (token.ElementAt(0) == '/')
            {
                token.RemoveAt(0);
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '*' || token.ElementAt(index) == '/'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr value = lvalue / term_2(lval);
                return term_1_Tail(value, token);
            }
            else
            {
                return lvalue;
            }
        }

        private static Expr exprTail(Expr lvalue, List<int> token)
        {
            HashSet<int> op = new HashSet<int>() { '*', '/', '^' };
            int br_num = 0;
            int index = 0;
            bool condition = true;
            List<int> lval = new List<int>();
            if (index == token.Count)
            {
                return lvalue;
            }
            else if (token.ElementAt(0) == '+')
            {
                token.RemoveAt(0);
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '+' || token.ElementAt(index) == '-'))
                        condition = true;
                    else if (br_num == 0 && (((token.ElementAt(index) == '-' && op.Contains(token.ElementAt(index - 1))) || (token.ElementAt(index) == '+') && op.Contains(token.ElementAt(index - 1)))))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '+' || token.ElementAt(index) == '-'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr value = lvalue + term_1(lval);
                return exprTail(value, token);
            }
            else if (token.ElementAt(0) == '-')
            {
                token.RemoveAt(0);
                while (condition)
                {
                    if (token.ElementAt(index) == '(')
                        ++br_num;
                    else if (token.ElementAt(index) == ')')
                        --br_num;

                    lval.Add(token.ElementAt(index));
                    ++index;

                    if (index == token.Count)
                        condition = false;
                    else if (br_num > 0 && (token.ElementAt(index) == '+' || token.ElementAt(index) == '-'))
                        condition = true;
                    else if (br_num == 0 && (((token.ElementAt(index) == '-' && op.Contains(token.ElementAt(index - 1))) || (token.ElementAt(index) == '+') && op.Contains(token.ElementAt(index - 1)))))
                        condition = true;
                    else if (br_num == 0 && (token.ElementAt(index) == '+' || token.ElementAt(index) == '-'))
                        condition = false;
                }
                token.RemoveRange(0, index);
                Expr value = lvalue - term_1(lval);
                return exprTail(value, token);
            }
            else
            {
                return lvalue;
            }
        }

        /*
         * tokenizer: Number (oct, hex, deci) --> NUM (128)
         *                 String ({a-zA-Z}{a-zA-Z0-9_}+) --> VAR (129)
         *                 PI, i --> CONST(130)
         *                 Operator ( '*', '/', '^', ',', '(', '+', '-', ')' ) --> ( '*', '/', '^', ',', '(', '+', '-', ')' )
         *                 ! --> fac()
         *                 End --> $
         *                 Else --> WRONG!!
         * */
        // 解析得到的token放入List<int> tokens 中，值放入 Queue<Expr> symbols 中
        private static void tokenizer(string str)
        {
            int index = 0;
            str = str + '$';
            int token;

            do
            {
                if (str[index] == ' ')
                    ++index;
                else if (str[index] >= '0' && str[index] <= '9') //解析数字
                {
                    string num_token = "";
                    if (str[index] == '0')
                    {
                        num_token += str[index];
                        ++index;
                        if (str[index] == '.')
                        {
                            num_token += str[index];
                            ++index;
                            while ((str[index] >= '0' && str[index] <= '9'))
                            {
                                num_token += str[index];
                                ++index;
                            }
                            token = NUM;
                            tokens.Add(token);
                            symbols.Enqueue(Util.parseNum(num_token));
                        }
                        else if ((str[index] == 'x' || str[index] == 'X'))
                        {
                            num_token += str[index];
                            ++index;
                            while ((str[index] >= '0' && str[index] <= '9') || (str[index] >= 'a' && str[index] <= 'f') || (str[index] >= 'A' && str[index] <= 'F'))
                            {
                                num_token += str[index];
                                ++index;
                            }
                            token = NUM;
                            tokens.Add(token);
                            symbols.Enqueue(Util.parseNum(num_token));
                        }
                        else
                        {
                            while ((str[index] >= '0' && str[index] <= '9'))
                            {
                                num_token += str[index];
                                ++index;
                            }
                            token = NUM;
                            tokens.Add(token);
                            symbols.Enqueue(Util.parseNum(num_token));
                        }
                    }
                    else
                    {
                        num_token += str[index];
                        ++index;
                        while ((str[index] >= '0' && str[index] <= '9'))
                        {
                            num_token += str[index];
                            ++index;
                        }

                        if (str[index] == '.')
                        {
                            num_token += str[index];
                            ++index;
                            while ((str[index] >= '0' && str[index] <= '9'))
                            {
                                num_token += str[index];
                                ++index;
                            }
                            token = NUM;
                            tokens.Add(token);
                            symbols.Enqueue(Util.parseNum(num_token));
                        }
                        else
                        {
                            token = NUM;
                            tokens.Add(token);
                            symbols.Enqueue(Util.parseNum(num_token));
                        }
                    }
                }
                else if ((str[index] >= 'a' && str[index] <= 'z') || (str[index] >= 'A' && str[index] <= 'Z') || str[index] == '_') //解析字符串
                {
                    string str_token = "";
                    str_token += str[index];
                    ++index;
                    while ((str[index] >= 'a' && str[index] <= 'z') || (str[index] >= 'A' && str[index] <= 'Z') || (str[index] >= '0' && str[index] <= '9') || str[index] == '_')
                    {
                        str_token += str[index];
                        ++index;
                    }
                    Expr var;
                    if (str_token == "PI")
                    {
                        token = CONST;
                        tokens.Add(token);
                        var = Expr.Pi;
                        symbols.Enqueue(var);
                    }
                    else if (str_token == "i")
                    {
                        token = CONST;
                        tokens.Add(token);
                        var = Expr.I;
                        symbols.Enqueue(var);
                    }
                    else
                    {
                        token = VAR;
                        tokens.Add(token);
                        var = Expr.Symbol(str_token);
                        symbols.Enqueue(var);
                    }
                }
                else if (ops.Contains(str[index])) //解析符号
                {
                    token = str[index];
                    tokens.Add(token);
                    ++index;
                }
                else if (str[index] == '!')
                {
                    Stack<Expr> stack = new Stack<Expr>();
                    int t = tokens.Last();
                    tokens.RemoveAt(tokens.Count() - 1);
                    tokens.Add(VAR);
                    tokens.Add('(');
                    tokens.Add(t);
                    tokens.Add(')');

                    while (symbols.Count > 0)
                        stack.Push(symbols.Dequeue());
                    while (stack.Count > 0)
                        symbols.Enqueue(stack.Pop());

                    Expr te = symbols.Dequeue();

                    while (symbols.Count > 0)
                        stack.Push(symbols.Dequeue());
                    while (stack.Count > 0)
                        symbols.Enqueue(stack.Pop());

                    symbols.Enqueue(Expr.Symbol("fac"));
                    symbols.Enqueue(te);
                    ++index;
                }
            }
            while (str[index] != '$');
        }
        //求符号微分
        public static Expr Diff(Expr arg, Expr input)
        {
            Expr output = Expr.Zero, output1 = Expr.Zero, output2 = Expr.Zero;
            int number = Util.numberOfTerm(input);
            Queue<Expr> queue = Util.extractTermFromExpr(input);

            if (number == 0)
                return Expr.Undefined;
            else if (number == 1)
            {
                string temp = Infix.Format(input);
                temp = temp.Replace('j', 'i');
                if (temp[0] == '-')
                {
                    temp = temp.Substring(1);
                    output += Expr.Zero - Diff(arg, parse(temp));
                }
                else if (input.IsNumber || input.IsConstant)
                    output += Expr.Zero;
                else if (input.IsFunction)
                {
                    int ind = temp.IndexOf('(');
                    string func_name = new string(temp.Take(ind).ToArray());
                    Expr temp_expr = parse(temp.Remove(temp.Length - 1, 1).Substring(ind + 1));
                    switch (func_name)
                    {
                        case "asin": output += Expr.Invert(Expr.Root(2, 1 - Expr.Pow(temp_expr, 2))) * Diff(arg, temp_expr); break;
                        case "acos": output += -Expr.Invert(Expr.Root(2, 1 - Expr.Pow(temp_expr, 2))) * Diff(arg, temp_expr); break;
                        case "atan": output += Expr.Invert(1 + Expr.Pow(temp_expr, 2)) * Diff(arg, temp_expr); break;
                        default: output += Calculus.Differentiate(temp_expr, input) * Diff(arg, temp_expr); ; break;
                    }
                }
                else if (input.IsIdentifier)
                {
                    int ind = temp.IndexOf('(');
                    if (ind == -1)
                        output += Calculus.Differentiate(arg, input);
                    else
                    {
                        string func_name = new string(temp.Take(ind).ToArray());
                        if (func_name == "Integrate")
                        {
                            string temp_s = temp.Substring(ind + 1);
                            int ind_p = temp_s.IndexOf(',');
                            string func = new string(temp_s.Take(ind_p).ToArray());
                            output += parse(func);
                        }
                        else
                        {
                            Expr temp_e = parse(temp.Remove(temp.Length - 1, 1).Substring(ind + 1));
                            output += Expr.Symbol(Infix.Format(input).Insert(Infix.Format(input).IndexOf('('), "'")) * Diff(arg, temp_e);
                        }
                    }
                }
                else if (input.IsProduct)
                {
                    int index = 0;
                    int br_num = 0;
                    bool condition = true;
                    string temp_1 = "";
                    string temp_2 = "";
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_1 += temp[index];
                        ++index;

                        if (index == temp.Length)
                            condition = false;
                        else if (br_num > 0 && (temp[index] == '*' || temp[index] == '/'))
                            condition = true;
                        else if (br_num == 0 && (temp[index] == '*' || temp[index] == '/'))
                            condition = false;
                    }
                    temp_2 = temp.Substring(index + 1);

                    Expr temp_e1 = parse(temp_1);
                    Expr temp_e2 = parse(temp_2);
                    if (temp[index] == '*')
                        output += Diff(arg, temp_e1) * temp_e2 + temp_e1 * Diff(arg, temp_e2);
                    else
                        output += Diff(arg, temp_e1) * Expr.Invert(temp_e2) + temp_e1 * Diff(arg, Expr.Invert(temp_e2));
                }
                else if (input.IsPower)
                {
                    int index = 0;
                    int br_num = 0;
                    bool condition = true;
                    string temp_1 = "";
                    string temp_2 = "";
                    while (condition)
                    {
                        if (temp[index] == '(')
                            ++br_num;
                        else if (temp[index] == ')')
                            --br_num;

                        temp_1 += temp[index];
                        ++index;

                        if (index == temp.Length)
                            condition = false;
                        else if (br_num > 0 && temp[index] == '/')
                            condition = true;
                        else if (br_num == 0 && temp[index] == '/')
                            condition = false;
                    }
                    Expr temp_e1 = parse(temp_1);

                    if (temp_e1.IsPower)
                    {
                        index = 0;
                        br_num = 0;
                        condition = true;
                        temp_1 = "";
                        temp_2 = "";
                        while (condition)
                        {
                            if (temp[index] == '(')
                                ++br_num;
                            else if (temp[index] == ')')
                                --br_num;

                            temp_1 += temp[index];
                            ++index;

                            if (index == temp.Length)
                                condition = false;
                            else if (br_num > 0 && (temp[index] == '^'))
                                condition = true;
                            else if (br_num == 0 && (temp[index] == '^'))
                                condition = false;
                        }
                        temp_2 = temp.Substring(index + 1);

                        Expr temp_expr1 = parse(temp_1);
                        Expr temp_expr2 = parse(temp_2);
                        output += Expr.Pow(temp_expr1, temp_expr2) * (Diff(arg, temp_expr2) * Expr.Ln(temp_expr1) + temp_expr2 * Diff(arg, Expr.Ln(temp_expr1)));
                    }
                    else
                    {
                        temp_2 = temp.Substring(index + 1);
                        Expr temp_e2 = parse(temp_2);
                        output += (input / temp_e2) * (Expr.Zero - Diff(arg, temp_e2));
                    }
                }
            }
            else
            {
                for (int i = 0; i < number; i++)
                {
                    Expr temp = queue.Dequeue();
                    output += Diff(arg, temp);
                }
            }
            return output;
        }

        private static double findroot(Expr arg, Expr input, double lower, double upper)
        {
            Func<double, double> f = xx =>
            {
                double result;
                Dictionary<string, FloatingPoint> sym = new Dictionary<string, FloatingPoint>();
                sym.Add(Infix.Format(arg), xx);
                if (Eval.Evaluate(sym, input).IsReal)
                    result = Eval.Evaluate(sym, input).RealValue;
                else
                    result = Eval.Evaluate(sym, input).ComplexValue.Real;
                return result;
            };
            Func<double, double> df = dx =>
            {
                double result;
                Dictionary<string, FloatingPoint> sym = new Dictionary<string, FloatingPoint>();
                sym.Add(Infix.Format(arg), dx);
                if (Eval.Evaluate(sym, Diff(arg, input)).IsReal)
                    result = Eval.Evaluate(sym, Diff(arg, input)).RealValue;
                else
                    result = Eval.Evaluate(sym, Diff(arg, input)).ComplexValue.Real;
                return result;
            };

            return MathNet.Numerics.RootFinding.RobustNewtonRaphson.FindRoot(f, df, lower, upper);
        }

        public static string findRoot(Expr arg, Expr input, double lower, double upper)
        {
            double result;
            try
            {
                result = findroot(arg, input, lower, upper);
                return result.ToString();
            }
            catch
            {
                return "There is NO root in the given INTERVAL!";
            }
        }
        //求解一重定积分
        public static double Integrate(Expr arg, Expr input, double begin, double end)
        {
            Func<double, double> f = xx =>
            {
                Dictionary<string, FloatingPoint> sym = new Dictionary<string, FloatingPoint>();
                sym.Add(Infix.Format(arg), xx);
                return Eval.Evaluate(sym, input).RealValue;
            };
            return MathNet.Numerics.Integration.GaussLegendreRule.Integrate(f, begin, end, 20);
        }
        //求解二重定积分
        public static double Integrate(Expr arg1, Expr arg2, Expr input, double begin1, double end1, double begin2, double end2)
        {
            Func<double, double, double> f = (x1, x2) =>
            {
                Dictionary<string, FloatingPoint> sym = new Dictionary<string, FloatingPoint>();
                sym.Add(Infix.Format(arg1), x1);
                sym.Add(Infix.Format(arg2), x2);
                return Eval.Evaluate(sym, input).RealValue;
            };
            return MathNet.Numerics.Integrate.OnRectangle(f, begin1, end1, begin2, end2, 20);
        }
        //模拟退火算法（仿照GSL库）
        public class SimulatedAnnealing
        {
            public class Params
            {
                public int N_TRIES;
                public int ITERS;
                public double T_MIN;
                public double T_INITIAL;
                public double T_FACTOR;
                public double K;
            }

            private static double boltzmann(double E, double new_E, double T, double k)
            {
                double x = -(new_E - E) / (k * T);
                return (x < LOG_DBL_MIN) ? 0.0 : Math.Exp(x);
            }

            public T SimAn<T>(T x0, Func<T, double> E_func, Func<Random, T, T> take_step, Func<T, string> print, Func<T, T> copy_func, Params p)
            {
                int OUTloop = p.N_TRIES;
                int INloop = p.ITERS;
                double t_min = p.T_MIN;
                double t_initial = p.T_INITIAL;
                double t = t_initial;
                double t_factor = p.T_FACTOR;
                double k = p.K;
                double E, new_E, best_E;
                int n_evals = 1, n_iter = 0, n_accepts, n_rejects, n_eless;
                T x, new_x, best_x;
                Random r = new Random();
                n_accepts = n_rejects = n_eless = 0;

                x = copy_func(x0);
                new_x = copy_func(x0);
                best_x = copy_func(x0);

                E = E_func(x0);
                best_E = E;

                while (true)
                {
                    for (int i = 0; i < INloop; ++i)
                    {
                        new_x = copy_func(x);
                        new_x = take_step(r, new_x);
                        new_E = E_func(new_x);

                        if (new_E < best_E)
                        {
                            best_x = copy_func(new_x);
                            best_E = new_E;
                        }

                        ++n_evals;

                        if (new_E < E)
                        {
                            if (new_E < best_E)
                            {
                                best_x = copy_func(new_x);
                                best_E = new_E;
                            }
                            x = copy_func(new_x);
                            E = new_E;
                            ++n_eless;
                        }
                        else if (r.NextDouble() < boltzmann(E, new_E, t, k))
                        {
                            x = copy_func(new_x);
                            E = new_E;
                            ++n_accepts;
                        }
                        else
                            ++n_rejects;
                    }

                    if (print.ToString() != null)
                    {
                        string text = n_iter.ToString() + ", " + t.ToString("0.000000") + " " + print(x) + E.ToString("0.00") + ", " + best_E.ToString("0.00");
                        Console.WriteLine(text);
                    }

                    t *= t_factor;
                    ++n_iter;
                    if (t < t_min)
                        break;
                }
                x0 = best_x;
                return x0;
            }

            public static double city_distance(Tuple<string, double, double> c1, Tuple<string, double, double> c2)
            {
                const double earth_radius = 6375.000;

                double sla1 = Math.Sin(c1.Item2 * Math.PI / 180), cla1 = Math.Cos(c1.Item2 * Math.PI / 180),
                    slo1 = Math.Sin(c1.Item3 * Math.PI / 180), clo1 = Math.Cos(c1.Item3 * Math.PI / 180);
                double sla2 = Math.Sin(c2.Item2 * Math.PI / 180), cla2 = Math.Cos(c2.Item2 * Math.PI / 180),
                    slo2 = Math.Sin(c2.Item3 * Math.PI / 180), clo2 = Math.Cos(c2.Item3 * Math.PI / 180);

                double x1 = cla1 * clo1;
                double x2 = cla2 * clo2;

                double y1 = cla1 * slo1;
                double y2 = cla2 * slo2;

                double z1 = sla1;
                double z2 = sla2;

                double dot_product = x1 * x2 + y1 * y2 + z1 * z2;

                double angle = Math.Acos(dot_product);

                return angle * earth_radius;
            }

            public int[] best_route = new int[12];
            public double best_E = 1.0e100;
            public int[] second_route = new int[12];
            public double second_E = 1.0e100;
            public int[] third_route = new int[12];
            public double third_E = 1.0e100;

            public void do_all_perms(int[] route, int n, Func<int[], double> E_func, Func<int[], int[]> copy_func, int N)
            {
                if (n == (N - 1))
                {
                    double E;
                    E = E_func(route);
                    if (E < best_E)
                    {
                        third_E = second_E;
                        third_route = copy_func(second_route);
                        second_E = best_E;
                        second_route = copy_func(best_route);
                        best_E = E;
                        best_route = copy_func(route);
                    }
                    else if (E < second_E)
                    {
                        third_E = second_E;
                        third_route = copy_func(second_route);
                        second_E = E;
                        second_route = copy_func(route);
                    }
                    else if (E < third_E)
                    {
                        third_E = E;
                        route = copy_func(third_route);
                    }
                }
                else
                {
                    int[] new_route = new int[N];
                    int swap_tmp;
                    new_route = copy_func(route);
                    for (int i = n; i < N; ++i)
                    {
                        swap_tmp = new_route[i];
                        new_route[i] = new_route[n];
                        new_route[n] = swap_tmp;
                        do_all_perms(new_route, n + 1, E_func, copy_func, N);
                    }
                }
            }
        }
        //以下3个函数处理复数运算
        public static Tuple<string, string> im_solver(string input)
        {
            Expr temp_e = parse(input);
            string temp = Infix.Format(temp_e);

            Func<string, bool> test_i = str =>
            {
                bool b = false;
                int n = str.Length;
                for (int i = 0; i < n; ++i)
                {
                    if ((str[i] >= 'a' && str[i] <= 'i') || (str[i] >= 'k' && str[i] <= 'z') || (str[i] >= 'A' && str[i] <= 'Z'))
                        b = b | true;
                }
                return b;
            };

            bool condition = test_i(temp);

            if (condition)
            {
                Queue<string> queue = Util.extractTermFromExpr(input);
                Complex cp = new Complex(0, 0);

                while (queue.Count > 0)
                {
                    string tmp = queue.Dequeue();
                    if (test_i(tmp))
                        cp += Util.complex_handle(Eval.Evaluate(null, parse(tmp)).ComplexValue);
                    else
                        cp += Util.complex_handle(new Complex(Eval.Evaluate(null, parse(trans_im(tmp).Item1)).RealValue, Eval.Evaluate(null, parse(trans_im(tmp).Item2)).RealValue));
                }
                return new Tuple<string, string>(cp.Real.ToString(), cp.Imaginary.ToString());
            }
            else
            {
                input = input.Replace('i', 'j');
                return trans_im(input);
            }
        }

        public static Tuple<string, string> trans_im(string input)
        {
            string re = "0";
            string im = "0";
            string temp_e = "0";
            Queue<string> queue = Util.extractTermFromExpr(input);

            Func<string, string, string, Tuple<string, string>> f_in = (t, r, i) =>
            {
                int ind = t.IndexOf('j');
                if (ind == -1)
                    r = Infix.Format(parse(r) + parse(t));
                else
                {
                    string t_3 = new string(t.Take(3).ToArray());
                    string t_4 = new string(t.Take(4).ToArray());
                    if (t_4 == "sqrt")
                    {
                        string arg = t.Remove(t.Length - 1, 1).Substring(5);
                        Tuple<string, string> tp = im_solver(arg);
                        Complex c = new Complex(Eval.Evaluate(null, parse(tp.Item1)).RealValue, Eval.Evaluate(null, parse(tp.Item2)).RealValue);
                        c = Complex.Sqrt(c);
                        r = Infix.Format(parse(r) + parse(c.Real.ToString()));
                        i = Infix.Format(parse(i) + parse(c.Imaginary.ToString()));
                    }
                    else if (t_3 == "jnv")
                    {
                        string arg = t.Remove(t.Length - 1, 1).Substring(4);
                        Tuple<string, string> tp = im_solver(arg);
                        Complex c = new Complex(Eval.Evaluate(null, parse(tp.Item1)).RealValue, Eval.Evaluate(null, parse(tp.Item2)).RealValue);
                        c = Complex.Divide(new Complex(1, 0), c);
                        r = Infix.Format(parse(r) + parse(c.Real.ToString()));
                        i = Infix.Format(parse(i) + parse(c.Imaginary.ToString()));
                    }
                    else if (t_4 == "root")
                    {
                        string arg = t.Remove(t.Length - 1, 1).Substring(5);
                        int ind_c = arg.IndexOf(',');
                        string arg1 = new string(arg.Take(ind_c).ToArray()).Trim(new char[] { ' ' });
                        string arg2 = arg.Substring(ind_c + 1).Trim(new char[] { ' ' });
                        Tuple<string, string> tp = im_solver(arg2);
                        Complex c = new Complex(Eval.Evaluate(null, parse(tp.Item1)).RealValue, Eval.Evaluate(null, parse(tp.Item2)).RealValue);
                        c = Complex.Pow(c, new Complex(Eval.Evaluate(null, Expr.Invert(parse(arg1))).RealValue, 0));
                        r = Infix.Format(parse(r) + parse(c.Real.ToString()));
                        i = Infix.Format(parse(i) + parse(c.Imaginary.ToString()));
                    }
                    else if (t_3 == "pow")
                    {
                        string arg = t.Remove(t.Length - 1, 1).Substring(4);
                        int ind_c = arg.IndexOf(',');
                        string arg1 = new string(arg.Take(ind_c).ToArray()).Trim(new char[] { ' ' });
                        string arg2 = arg.Substring(ind_c + 1).Trim(new char[] { ' ' });
                        Tuple<string, string> tp = im_solver(arg1);
                        Complex c = new Complex(Eval.Evaluate(null, parse(tp.Item1)).RealValue, Eval.Evaluate(null, parse(tp.Item2)).RealValue);
                        c = Complex.Pow(c, new Complex(Eval.Evaluate(null, parse(arg2)).RealValue, 0));
                        r = Infix.Format(parse(r) + parse(c.Real.ToString()));
                        i = Infix.Format(parse(i) + parse(c.Imaginary.ToString()));
                    }
                    else
                    {
                        t = t.Remove(ind, 1);
                        t = t.Insert(ind, "1");
                        i = Infix.Format(parse(i) + parse(t));
                    }
                }
                return new Tuple<string, string>(r, i);
            };

            while (queue.Count > 0)
            {
                string tmp_s = queue.Dequeue();
                if (tmp_s.Contains('j'))
                {
                    temp_e = simplify_im_pow(tmp_s);
                    Queue<string> q = Util.extractTermFromExpr(temp_e);

                    if (q.Count == 1)
                    {
                        string tmp = q.Dequeue();
                        Tuple<string, string> tp = f_in(tmp, re, im);
                        re = tp.Item1;
                        im = tp.Item2;
                    }
                    else if (q.Count == 2)
                    {
                        string tmp0 = q.Dequeue();
                        Tuple<string, string> tp0 = f_in(tmp0, re, im);
                        re = tp0.Item1;
                        im = tp0.Item2;
                        string tmp1 = q.Dequeue();
                        Tuple<string, string> tp1 = f_in(tmp1, re, im);
                        re = tp1.Item1;
                        im = tp1.Item2;
                    }
                }
                else
                    re = Infix.Format(parse(re) + parse(tmp_s));
            }

            re = Eval.Evaluate(null, parse(re)).RealValue.ToString();
            im = Eval.Evaluate(null, parse(im)).RealValue.ToString();

            return new Tuple<string, string>(re, im);
        }

        public static string simplify_im_pow(string input)
        {
            string pattern = @"a-ik-zA-Z";

            if (!input.Contains('j'))
                return input;

            char[] cs = new char[] { '*', '/', '^' };
            string[] ss = spliter(input, cs);
            string op = ss[0];
            string left = ss[1];
            string right = ss[2];

            Func<string, string, string, string> expr_to_complex = (c, l, r) =>
            {
                Complex oc;
                Tuple<string, string> tl = trans_im(l);
                Tuple<string, string> tr = trans_im(r);
                double lr = Eval.Evaluate(null, parse(tl.Item1)).RealValue;
                double li = Eval.Evaluate(null, parse(tl.Item2)).RealValue;
                double rr = Eval.Evaluate(null, parse(tr.Item1)).RealValue;
                double ri = Eval.Evaluate(null, parse(tr.Item2)).RealValue;
                if (c == "^")
                    oc = Complex.Pow(new Complex(lr, li), new Complex(rr, ri));
                else if (c == "*")
                    oc = Complex.Multiply(new Complex(lr, li), new Complex(rr, ri));
                else if (c == "/")
                    oc = Complex.Divide(new Complex(lr, li), new Complex(rr, ri));
                else
                    oc = new Complex(lr, li);
                return "(" + oc.Real.ToString() + ") + (" + oc.Imaginary.ToString() + ") * j";
            };

            if (op == "^")
            {
                if (right == "")
                    return input;
                else
                {
                    foreach (char c in right)
                    {
                        if (Regex.Match(c.ToString(), pattern).Success)
                            return input;
                    }
                    return expr_to_complex(op, left, right);
                }
            }
            else if (op == "*")
                return expr_to_complex(op, left, right);
            else if (op == "/")
                return expr_to_complex(op, left, right);
            else
                return left;
        }

        public static string[] spliter(string ss, char c)
        {
            int index = 0;
            int br_num = 0;
            bool condition = true;
            string temp_1 = "";
            string temp_2 = "";
            while (condition)
            {
                if (ss[index] == '(')
                    ++br_num;
                else if (ss[index] == ')')
                    --br_num;

                temp_1 += ss[index];
                ++index;

                if (index == ss.Length)
                    condition = false;
                else if (br_num > 0 && ss[index] == c)
                    condition = true;
                else if (br_num == 0 && ss[index] == c)
                    condition = false;
            }
            if (index == ss.Length)
                temp_2 = "";
            else
                temp_2 = ss.Substring(index + 1);


            if (temp_1.First() == '(' && temp_1.Last() == ')')
                temp_1 = temp_1.Remove(temp_1.LastIndexOf(')'), 1).Remove(0, 1);
            if (temp_2 != "" && temp_2.First() == '(' && temp_2.Last() == ')')
                temp_2 = temp_2.Remove(temp_2.LastIndexOf(')'), 1).Remove(0, 1);

            return new string[2] { temp_1, temp_2 };
        }

        public static string[] spliter(string ss, char[] cs)
        {
            int index = 0;
            int br_num = 0;
            bool condition = true;
            string temp_1 = "";
            string temp_2 = "";
            string op;
            while (condition)
            {
                if (ss[index] == '(')
                    ++br_num;
                else if (ss[index] == ')')
                    --br_num;

                temp_1 += ss[index];
                ++index;

                if (index == ss.Length)
                    condition = false;
                else if (br_num > 0 && cs.Contains(ss[index]))
                    condition = true;
                else if (br_num == 0 && cs.Contains(ss[index]))
                    condition = false;
            }
            if (index == ss.Length)
            {
                op = "";
                temp_2 = "";
            }
            else
            {
                temp_2 = ss.Substring(index + 1);
                op = ss[index].ToString();
            }


            if (temp_1.First() == '(' && temp_1.Last() == ')')
                temp_1 = temp_1.Remove(temp_1.LastIndexOf(')'), 1).Remove(0, 1);
            if (temp_2 != "" && temp_2.First() == '(' && temp_2.Last() == ')')
                temp_2 = temp_2.Remove(temp_2.LastIndexOf(')'), 1).Remove(0, 1);

            return new string[3] { op, temp_1, temp_2 };
        }

        public static string Solve(string input)
        {
            string output = "";
            int index_c = input.IndexOf(';');
            int index_e = input.IndexOf('=');
            Expr ex;
            HashSet<Expr> set;

            if (index_c == -1 && index_e == -1)
            {
                char first = input.First();
                if (input.Contains('D'))
                    output = diff_solver(input);
                else
                {
                    string first_2 = new string(input.Take(2).ToArray());
                    string first_4 = new string(input.Take(4).ToArray());
                    string first_5 = new string(input.Take(5).ToArray());
                    if (first_2 == "re" || first_2 == "im")
                    {
                        string temp = input.Remove(input.Length - 1).Substring(3);
                        try
                        {
                            Tuple<string, string> tp = im_solver(temp);
                            if (first_2 == "re")
                                output = tp.Item1;
                            else
                                output = tp.Item2;
                        }
                        catch
                        {
                            output = "UNRESOLVED arguments in the input equation!";
                        }
                    }
                    else if (first_4 == "ceil")
                    {
                        string temp = input.Remove(input.Length - 1).Substring(5);
                        try
                        {
                            output = Math.Ceiling(Eval.Evaluate(null, parse(temp)).RealValue).ToString();
                        }
                        catch
                        {
                            output = "WRONG input!";
                        }
                    }
                    else if (first_5 == "floor")
                    {
                        string temp = input.Remove(input.Length - 1).Substring(6);
                        try
                        {
                            output = Math.Floor(Eval.Evaluate(null, parse(temp)).RealValue).ToString();
                        }
                        catch
                        {
                            output = "WRONG input!";
                        }
                    }
                    else
                    {
                        ex = parse(input);
                        Queue<string> queue = Util.extractTermFromExpr(input);
                        Queue<string> q = new Queue<string>();
                        while (queue.Count > 0)
                        {
                            string temp = "";
                            string tmp_s = queue.Dequeue();
                            Expr tmp_e = parse(tmp_s);
                            if (Infix.Format(tmp_e).Contains('j'))
                            {
                                try
                                {
                                    Tuple<string, string> tp = im_solver(tmp_s);
                                    if (tp.Item1 != "0" && tp.Item2 != "0")
                                    {
                                        if (tp.Item2 != "1")
                                            temp = tp.Item1 + " + " + tp.Item2 + " * i";
                                        else
                                            temp = tp.Item1 + " + " + "i";
                                    }
                                    else if (tp.Item1 == "0" && tp.Item2 != "0")
                                    {
                                        if (tp.Item2 != "1")
                                            temp = tp.Item2 + " * i";
                                        else
                                            temp = "i";
                                    }
                                    else if (tp.Item1 != "0" && tp.Item2 == "0")
                                        temp = tp.Item1;
                                    else
                                        temp = "0";
                                }
                                catch
                                {
                                    temp = tmp_s;
                                }
                            }
                            else
                            {
                                set = Util.collectSymbols(tmp_e);
                                if (set.Count == 0)
                                    temp = Eval.Evaluate(null, tmp_e).RealValue.ToString();
                                else
                                    temp = Infix.Format(tmp_e);
                            }
                            q.Enqueue(temp);
                        }

                        while (q.Count() > 1)
                            output += q.Dequeue() + " + ";
                        output += q.Dequeue();
                    }
                }
            }
            else if (index_c == -1 && index_e != -1)
            {
                string left = new string(input.Take(index_e).ToArray());
                string right = input.Substring(index_e + 1).Trim(new char[] { ' ' });
                Expr left_e = parse(left);
                Expr right_e = parse(right);
                Expr total = left_e - right_e;
                HashSet<Expr> funcs = new HashSet<Expr>();
                HashSet<Expr> args = new HashSet<Expr>();
                set = Util.collectSymbols(total);
                foreach (Expr s in set)
                {
                    if (Infix.Format(s).Contains('('))
                        funcs.Add(s);
                    else
                        args.Add(s);
                }

                if (args.Count == 0)
                {
                    Complex c = Eval.Evaluate(null, total).ComplexValue;
                    if (c.Real == 0 && c.Imaginary == 0)
                        output = "0 = 0";
                    else
                        output = "Wrong Eqution!";
                }
                else if (args.Count == 1 && Polynomial.IsPolynomial(args.First(), total))
                {
                    Complex[] cs = Util.eqSolver(args.First(), total);
                    if (cs == null)
                        output = "Only Solve for 2, 3 and 4 ORDER Equation!";
                    else
                    {
                        int n = cs.Length;
                        for (int i = 0; i < n; i++)
                            output += "x" + (i + 1) + " = " + cs[i].Real.ToString("0.0000") + " + " + cs[i].Imaginary.ToString("0.0000") + " * i; ";
                    }
                }
                else
                    output = "WRONG input!";
            }
            else if (index_c != -1 && index_e == -1)
            {
                string tmp = new string(input.Take(9).ToArray());
                if (tmp == "Integrate")
                {
                    string[] tmps = input.Split(new char[] { ';' });
                    if (tmps.Length == 2)
                    {
                        int index_colon = tmps[1].IndexOf(':');
                        string arg = new string(tmps[1].Take(index_colon).ToArray());
                        string[] interval = tmps[1].Remove(tmps[1].Length - 1).Substring(index_colon + 2).Split(new char[] { ',' });
                        string func = tmps[0].Remove(tmps[0].Length - 1).Substring(10);
                        Expr low = Util.parseNum(interval[0]);
                        Expr up = Util.parseNum(interval[1]);
                        Expr arg_e = parse(arg);
                        Expr func_e = parse(func);
                        double lower = Eval.Evaluate(null, low).RealValue;
                        double upper = Eval.Evaluate(null, up).RealValue;
                        output = Integrate(arg_e, func_e, lower, upper).ToString();
                    }
                    else if (tmps.Length == 3)
                    {
                        int index_colon1 = tmps[1].IndexOf(':');
                        string arg1 = new string(tmps[1].Take(index_colon1).ToArray());
                        string[] interval1 = tmps[1].Remove(tmps[1].Length - 1).Substring(index_colon1 + 2).Split(new char[] { ',' });
                        int index_colon2 = tmps[2].IndexOf(':');
                        string arg2 = new string(tmps[2].Take(index_colon2).ToArray());
                        string[] interval2 = tmps[2].Remove(tmps[2].Length - 1).Substring(index_colon2 + 2).Split(new char[] { ',' });
                        string func = tmps[0].Remove(tmps[0].Length - 1).Substring(10);
                        Expr low1 = Util.parseNum(interval1[0]);
                        Expr up1 = Util.parseNum(interval1[1]);
                        Expr low2 = Util.parseNum(interval2[0]);
                        Expr up2 = Util.parseNum(interval2[1]);
                        Expr arg_1 = parse(arg1);
                        Expr arg_2 = parse(arg2);
                        Expr func_e = parse(func);
                        double lower1 = Eval.Evaluate(null, low1).RealValue;
                        double upper1 = Eval.Evaluate(null, up1).RealValue;
                        double lower2 = Eval.Evaluate(null, low2).RealValue;
                        double upper2 = Eval.Evaluate(null, up2).RealValue;
                        output = Integrate(arg_2, arg_1, func_e, lower1, upper1, lower1, upper2).ToString();
                    }
                    else
                        output = "WRONG input!";
                }
                else
                    output = "WRONG input!";
            }
            else if (index_c != -1 && index_e != -1)
            {
                string[] tmps = input.Split(new char[] { ';' });
                if (tmps.Length == 2)
                {
                    int index_colon = tmps[1].IndexOf(':');
                    string arg = new string(tmps[1].Take(index_colon).ToArray());
                    string[] interval = tmps[1].Remove(tmps[1].Length - 1).Substring(index_colon + 2).Split(new char[] { ',' });
                    Expr arg_e = parse(arg);
                    Expr low = Util.parseNum(interval[0]);
                    Expr up = Util.parseNum(interval[1]);
                    double lower = Eval.Evaluate(null, low).RealValue;
                    double upper = Eval.Evaluate(null, up).RealValue;
                    string left = new string(tmps[0].Take(index_e).ToArray());
                    string right = tmps[0].Substring(index_e + 1);
                    Expr left_e = parse(left);
                    Expr right_e = parse(right);
                    Expr total = left_e - right_e;
                    string root = findRoot(arg_e, total, lower, upper);
                    if (root == "There is NO root in the given INTERVAL!")
                        output = root;
                    else
                        output = arg + " = " + root;
                }
                else
                    output = "WRONG input!";
            }
            else
                output = "WRONG input!";

            return output;
        }

        private static string diff_solver(string input)
        {
            string output = "";
            Queue<string> queue = Util.extractTermFromExpr(input);
            Queue<string> q = new Queue<string>();

            while(queue.Count() > 0)
            {
                string temp = queue.Dequeue();
                q.Enqueue(extract_D(temp));
            }

            while (q.Count > 1)
                output += q.Dequeue() + " + ";
            output = output + q.Dequeue();

            return output;
        }

        private static string extract_D(string in_str)
        {
            char[] os_in = new char[] { '*', '/', '^', ',', '+', '-' };
            int ind = in_str.IndexOf('D');
            if (ind == -1)
                return in_str;
            else
            {
                in_str = in_str.Substring(ind);
                string[] tmps1 = spliter(in_str, '/');
                string tmp1 = tmps1[0];
                string tmp1_in = tmp1.Remove(tmp1.Length - 1).Substring(2);
                if (tmp1_in.Contains('D'))
                    tmp1_in = diff_solver(tmp1_in);
                string tmp2 = tmps1[1];
                string[] tmps2 = spliter(tmp2, os_in);
                string tmp2_op = tmps2[0];
                string tmp2_left = tmps2[1];
                string tmp2_right = tmps2[2];
                string extract_out = "D" + "(" + tmp1_in + ")" + "/" + tmp2_left;
                if (tmp2_right.Contains('D'))
                    tmp2_right = diff_solver(tmp2_right);
                return diff_D(extract_out) + tmp2_op + tmp2_right;
            }
        }

        private static string diff_D(string str_in)
        {
            string[] temps = spliter(str_in, '/');
            string func = temps[0].Remove(temps[0].Length - 1).Substring(2);
            string arg = temps[1].Remove(temps[1].Length - 1).Substring(2);
            Expr func_e = parse(func);
            Expr arg_e = parse(arg);
            return Infix.Format(Diff(arg_e, func_e));
        }

    }
    
}
