using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static PX_InjectHelper.Inject_Helper;

namespace Class_Injector
{
    public partial class Form1 : Form
    {

        #region Variables
        string directoryName = "";
        string filePath = "";
        static ModuleDefMD module = null;
        public Thread thr;
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        
        private void AddAntiDump(ModuleDef module , string methodName)
        {
            ModuleDefMD typeModule = ModuleDefMD.Load(typeof(PX_AntiDump.AntiDump).Module);
            MethodDef injectMethod;
            injectMethod = null;
            if(radioButton1.Checked == true)
            {
                injectMethod = module.GlobalType.FindOrCreateStaticConstructor();
            }
            if(radioButton2.Checked == true)
            {
                injectMethod = module.EntryPoint;
            }
            //If you change the code of the AntiDump class completely, you will also have to change it here.
          
            TypeDef typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(PX_AntiDump.AntiDump).MetadataToken)); 
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, module.GlobalType, module); 

            MethodDef init = (MethodDef)members.Single(method => method.Name == methodName);

            injectMethod.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

            foreach (TypeDef type in module.Types)
            {
                if (type.IsGlobalModuleType || type.Name == "Resources" || type.Name == "Settings" || type.Name.Contains("Form"))
                    continue;
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    if (method.IsConstructor)
                    {
                        method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Nop));
                        method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                    }
                }
            }

            foreach (MethodDef md in module.GlobalType.Methods)
            {
                if (md.Name != ".ctor") continue;
                module.GlobalType.Remove(md);
                break;
            }
        }
        
        private void Button2_Click(object sender, EventArgs e)
        {
            if (filePath != string.Empty && module != null)
            {
                thr = new Thread(new ThreadStart(CodeBlock));
                thr.Start();
            }
        }
        public void CodeBlock()
        {
            //Modify the AntiDump class to change the desired class to be injected.

            AddAntiDump(module , "Initialize"); //Method Name and Module
            SaveAssembly();
            MessageBox.Show("Class Injected Successfully!", "Class Injector", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #region Github
        private void Label2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Rhotav");
        }
        #endregion


        #region Select
        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Executable Files|*.exe|DLL Files |*.dll";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    module = ModuleDefMD.Load(open.FileName);
                    filePath = open.FileName;
                    pictureBox1.BackColor = Color.Lime;
                    label4.Text = "Loaded !";
                    label4.ForeColor = Color.Lime;
                }
            }
            catch (Exception ex)
            {
                filePath = "";
                module = null;
                MessageBox.Show(ex.Message, "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pictureBox1.BackColor = Color.Red;
                label4.Text = "Not Loaded !";
                label4.ForeColor = Color.Lime;
            }
        }
        #endregion


        #region Save
        static void SaveAssembly()
        {
            var writerOptions = new NativeModuleWriterOptions(module, null);
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            writerOptions.MetaDataOptions.Flags = (MetaDataFlags.PreserveTypeRefRids | MetaDataFlags.PreserveTypeDefRids | MetaDataFlags.PreserveFieldRids | MetaDataFlags.PreserveMethodRids | MetaDataFlags.PreserveParamRids | MetaDataFlags.PreserveMemberRefRids | MetaDataFlags.PreserveStandAloneSigRids | MetaDataFlags.PreserveEventRids | MetaDataFlags.PreservePropertyRids | MetaDataFlags.PreserveTypeSpecRids | MetaDataFlags.PreserveMethodSpecRids | MetaDataFlags.PreserveStringsOffsets | MetaDataFlags.PreserveUSOffsets | MetaDataFlags.PreserveBlobOffsets | MetaDataFlags.PreserveAll | MetaDataFlags.AlwaysCreateGuidHeap | MetaDataFlags.PreserveExtraSignatureData | MetaDataFlags.KeepOldMaxStack);
            module.NativeWrite(Path.GetDirectoryName(module.Location) + @"\" + Path.GetFileNameWithoutExtension(module.Location) + "_inj.exe", writerOptions);
        }
        #endregion


        #region Drag

        private void Panel1_DragDrop_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Panel1_DragEnter_1(object sender, DragEventArgs e)
        {
            try
            {
                Array array = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (array != null)
                {
                    string text = array.GetValue(0).ToString();
                    int num = text.LastIndexOf(".");
                    if (num != -1)
                    {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".exe" || text2 == ".dll")
                        {
                            Activate();
                            int num2 = text.LastIndexOf("\\");
                            if (num2 != -1)
                            {
                                directoryName = text.Remove(num2, text.Length - num2);
                            }
                            if (directoryName.Length == 2)
                            {
                                directoryName += "\\";
                            }
                            module = ModuleDefMD.Load(text);
                            filePath = text;
                            pictureBox1.BackColor = Color.Lime;
                            label4.Text = "Loaded !";
                            label4.ForeColor = Color.Lime;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                filePath = "";
                module = null;
                MessageBox.Show(ex.Message, "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pictureBox1.BackColor = Color.Red;
                label4.Text = "Not Loaded !";
                label4.ForeColor = Color.Lime;
            }
        }
        #endregion
    }
}
