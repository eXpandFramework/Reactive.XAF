using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Input;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace Xpand.XAF.Modules.Windows.SystemActions {

    public class HotKeyForm:XtraForm {
        private bool _keyIsSet;
        public TextEdit TextEdit { get; private set; }

        public HotKeyForm() => InitComponent();

        private void InitComponent() {
            AutoSize = true;
            ControlBox = false;
            TextEdit = new TextEdit();
            TextEdit.KeyDown+=TextEditOnKeyDown;
            TextEdit.KeyUp+=TextEditOnKeyUp;
            Controls.Add(TextEdit);
            var cancelButton = new SimpleButton();
            Controls.Add(cancelButton);
            cancelButton.Top = TextEdit.Height + 1;
            cancelButton.Text = "Cancel";
            cancelButton.Click += (_, _) => {
                Close();
                Canceled = true;
            };
            var okButton = new SimpleButton();
            okButton.Click += (_, _) => Close();
            Controls.Add(okButton);
            okButton.Left = cancelButton.Width + 1;
            okButton.Text = "OK";
            okButton.Top = TextEdit.Height + 1;
            TextEdit.Width = okButton.Right;
            Width = TextEdit.Width + 2;
            Height = okButton.Bottom + 2;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterParent;
        }
        
        private void TextEditOnKeyUp(object sender, KeyEventArgs e) {
            if (!_keyIsSet ) {
                TextEdit.Text = Keys.None.ToString();
            }
        }

        private void TextEditOnKeyDown(object sender, KeyEventArgs e) {
            e.SuppressKeyPress = true;  
            TextEdit.Text = string.Empty; 
            _keyIsSet = false;
            if (e.KeyData == Keys.Back) {
                TextEdit.Text = Keys.None.ToString();
                return;
            }
            if (e.Modifiers == Keys.None&&e.KeyCode!=Keys.LWin) {
                MessageBox.Show("You have to specify a modifier like 'Control', 'Alt' or 'Shift'");
                TextEdit.Text = Keys.None.ToString();
                return;
            }
            foreach (var modifier in e.Modifiers.ToString().Split(new[] { ',' })) {
                TextEdit.Text += modifier + " + ";
            }

            if (Keyboard.IsKeyDown(Key.LWin)) {
                TextEdit.Text += "LWin + ";
            }
            if (e.KeyCode == Keys.ShiftKey | e.KeyCode == Keys.ControlKey | e.KeyCode == Keys.Menu||e.KeyCode==Keys.LWin) {
                _keyIsSet = false;
            }
            else {
                TextEdit.Text += e.KeyCode;
                _keyIsSet = true;
            }
        }
        
        public bool Canceled { get; private set; }

        public sealed override bool AutoSize {
            get => base.AutoSize;
            set => base.AutoSize = value;
        }
    }
    
    public class HotKeyEditor:UITypeEditor {
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            var hotKeyForm = new HotKeyForm();
            if (provider is ISupportLookAndFeel lookAndFeel)
                hotKeyForm.LookAndFeel.Assign(lookAndFeel.LookAndFeel);
            var hotKey = (string)context.PropertyDescriptor!.GetValue(context.Instance);
            hotKeyForm.TextEdit.Text = hotKey;
            hotKeyForm.ShowDialog((IWin32Window)provider);
            return hotKeyForm.Canceled ? hotKey : hotKeyForm.TextEdit.Text;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;
    }

}