using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Numerics;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.Expression;
using Eval = MathNet.Symbolics.Evaluate;

namespace mathapp
{
    public partial class MathApp : Form
    {

        public MathApp()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("1");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("4");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("6");
        }

        private void button_div_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("/");
        }

        private void button_asin_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "asin()";
            SendKeys.Send("{LEFT}");
        }

        private void button_sin_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "sin()";
            SendKeys.Send("{LEFT}");
        }

        private void button_r_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("{RIGHT}");
        }

        private void button_fac_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "!";
        }

        private void button_abs_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "abs()";
            SendKeys.Send("{LEFT}");
        }

        private void button_im_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "im()";
            SendKeys.Send("{LEFT}");
        }

        private void button_solve_Click(object sender, EventArgs e)
        {
            TextBox tb = new TextBox();
            tb.Text = textBox.Text;
            tb.Height = textBox.Height;
            tb.Width = textBox.Width;
            tb.Margin = textBox.Margin;
            tb.Location = textBox.Location;
            tb.Text = textBox.Text;
            tb.Font = textBox.Font;
            tb.Visible = true;
            tb.Parent = panel_text;
            textBox.Parent = panel_text;
            Point p = new Point();
            p.X = tb.Location.X;
            p.Y = tb.Location.Y + tb.Size.Height + tb.Margin.Top;
            textBox.Focus();
            textBox.Location = p;
            panel_text.ScrollControlIntoView(textBox);

            string tmp = tb.Text;

            string temp = new string(tmp.Take(9).ToArray());
            int index_c = tmp.IndexOf(';');
            int index_e = tmp.IndexOf('=');
            string result;
            if (temp == "Integrate" || (index_c != -1 && index_e != -1))
            {
                Task<string> t = new Task<string>(str => Member.Solve((string)str), tmp);
                t.Start();
                t.Wait();
                result = t.Result;
            }
            else
                result = Member.Solve(tmp);
            textBox.Clear();
            textBox.SelectionLength = 0;
            textBox.SelectedText = result;
        }

        private void button_l_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("{LEFT}");
        }

        private void button_enter_Click(object sender, EventArgs e)
        {
            TextBox tb = new TextBox();
            tb.Text = textBox.Text;
            tb.Height = textBox.Height;
            tb.Width = textBox.Width;
            tb.Margin = textBox.Margin;
            tb.Location = textBox.Location;
            tb.Text = textBox.Text;
            tb.Font = textBox.Font;
            tb.Visible = true;
            tb.Parent = panel_text;
            textBox.Parent = panel_text;
            Point p = new Point();
            p.X = tb.Location.X;
            p.Y = tb.Location.Y + tb.Size.Height + tb.Margin.Top;
            textBox.Clear();
            textBox.Focus();
            textBox.Location = p;
            panel_text.ScrollControlIntoView(textBox);
        }

        private void button_cos_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "cos()";
            SendKeys.Send("{LEFT}");
        }

        private void button_tan_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "tan()";
            SendKeys.Send("{LEFT}");
        }

        private void button_sinh_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "sinh()";
            SendKeys.Send("{LEFT}");
        }

        private void button_tanh_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "tanh()";
            SendKeys.Send("{LEFT}");
        }

        private void button_ln_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "ln()";
            SendKeys.Send("{LEFT}");
        }

        private void button_floor_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "floor()";
            SendKeys.Send("{LEFT}");
        }

        private void button_re_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "re()";
            SendKeys.Send("{LEFT}");
        }

        private void button_pi_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("PI");
        }

        private void button_diff_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "D()/D()";
            SendKeys.Send("{LEFT 5}");
        }

        private void button_acos_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "acos()";
            SendKeys.Send("{LEFT}");
        }

        private void button_atan_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "atan()";
            SendKeys.Send("{LEFT}");
        }

        private void button_cosh_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "cosh()";
            SendKeys.Send("{LEFT}");
        }

        private void button_exp_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "exp()";
            SendKeys.Send("{LEFT}");
        }

        private void button_log_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "log()";
            SendKeys.Send("{LEFT}");
        }

        private void button_ceil_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "ceil()";
            SendKeys.Send("{LEFT}");
        }

        private void button_i_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("i");
        }

        private void button_x_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("x");
        }

        private void button_y_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("y");
        }

        private void button_t_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("t");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("7");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("2");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("5");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("8");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("3");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("9");
        }

        private void button_dot_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send(".");
        }

        private void button0_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("0");
        }

        private void button_comma_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send(",");
        }

        private void button_add_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "+";
        }

        private void button_pow_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "^";
        }

        private void button_mult_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "*";
        }

        private void button_minus_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "-";
        }

        private void button_root_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "root()";
            SendKeys.Send("{LEFT}");
        }

        private void button_bra_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "()";
            SendKeys.Send("{LEFT}");
        }

        private void button_clear_Click(object sender, EventArgs e)
        {
            textBox.Clear();
        }

        private void button_del_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            SendKeys.Send("{BS}");
        }

        private void button_integrate_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "Integrate()";
            SendKeys.Send("{LEFT}");
        }

        private void button_sqrt_Click(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.SelectedText = "sqrt()";
            SendKeys.Send("{LEFT}");
        }
    }
}
