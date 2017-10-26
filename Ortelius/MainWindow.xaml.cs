using Microsoft.Win32;
using Saxon.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
//using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace dk.marten.ortelius
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static string startPath = "";
        private static string startName = "";
        private static string destinationPath = "";
        private string latestVersion = "";
        bool changeFlag = false;

        ProjectSettings projSettings;
        GenerelSettings appSettings;
        IDocumentationBuilder asDocumentation;

        private string systemSvar = "";

        private XmlDocument allDocXml;


        public enum CodeLanguage { Actionscript, Javascript };

        public MainWindow()
        {
            InitializeComponent();
            InitStuff();
        }

        //Initialize
        void InitStuff()
        {
            projSettings = new ProjectSettings();
            appSettings = new GenerelSettings();

            populateStyleCombo();
            changeFlag = false;
            Progress.Value = 0;
            //newVersion.Visible = false;
            //progress.Width = 0;
            //checkForUpdates();

        }


        /// <summary>
        /// Checking if there is a new version available
        /// </summary>
        private void checkForUpdates()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string version = asm.GetName().Version.ToString();
            //versionLabel.Text = version;
            string url = "http://ortelius.marten.dk/latest_version.aspx?version=" + version;

            try
            {
                WebClient client = new WebClient();
                latestVersion = client.DownloadString(url);
                if (latestVersion != version)
                {
                    //newVersion.Text = "Version " + latestVersion + " is available";
                    //newVersion.Visible = true;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show( ex.Message );
            }
        }

        void populateStyleCombo()
        {
            int basicIndex = 0;
            int count = 0;
            String[] Files = Directory.GetFileSystemEntries(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/styles");
            foreach (string Element in Files)
            {
                if (File.Exists(Element))
                {
                    if (Path.GetExtension(Element) == ".xsl")
                    {
                        if (Path.GetFileNameWithoutExtension(Element).IndexOf("OrteliusAjax") == 0) basicIndex = count;
                        StyleCB.Items.Add(Path.GetFileNameWithoutExtension(Element));
                        count++;
                    }
                }
            }
          
            StyleCB.SelectedIndex = basicIndex;
        }

        #region Add files


        ///<summary>
        /// Starter importen
        ///</summary>
        private void AddFileClick(object sender, RoutedEventArgs e)
        {

            //string fileName = "";
            string fileExt = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (projSettings.LastFolderName == "") openFileDialog1.InitialDirectory = "Desktop";
            else openFileDialog1.InitialDirectory = projSettings.LastFolderName;
            openFileDialog1.Multiselect = true;
            switch ((CodeLanguage)projSettings.Language)
            {
                case CodeLanguage.Actionscript:
                    openFileDialog1.Filter = "ActionScript files(*.as)|*.as";
                    openFileDialog1.Title = "Add ActionScript file ";
                    fileExt = ".as";
                    break;
                case CodeLanguage.Javascript:
                    openFileDialog1.Filter = "Javascript files(*.js)|*.js";
                    openFileDialog1.Title = "Add Javascript file ";
                    fileExt = ".js";
                    break;
            }
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog().Value)
            {
                projSettings.LastFolderName = Path.GetDirectoryName(openFileDialog1.FileName);
                if (openFileDialog1.FileNames != null)
                {
                    foreach (string fileName in openFileDialog1.FileNames)
                    {
                        if (projSettings.AddFile(fileName))
                        {
                            changeFlag = true;
                            renderList();
                        }
                        else if (Path.GetExtension(fileName) != fileExt) MessageBox.Show("Wrong file format: " + fileName);
                    }
                }
            }
        }

        ///<summary>
        /// Make the list of as files
        ///</summary>
        private void renderList()
        {
            //FileList.BeginUpdate();
            FileList.Items.Clear();
            foreach (string filNavn in projSettings.AllFiles)
            {
                FileList.Items.Add(filNavn);
            }
            //FileList.EndUpdate();
        }

        #endregion


        #region Build documentation

        void updateProgress(float value)
        {
            Progress.Dispatcher.Invoke(() => Progress.Value = value, DispatcherPriority.Background);
        }

        void BuildDocumentationClick(object sender, RoutedEventArgs  e)
        {

            if (projSettings.DestinationPath == "" || !Directory.Exists(projSettings.DestinationPath))
            {
                MessageBox.Show("You need to select a destination folder", "No destination folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            allDocXml = new XmlDocument();

            systemSvar = "";
            BuildButton.IsEnabled = false;

            updateProgress(2f);

            BuildDocumentationXml();

            updateProgress(40f);

            SaveXml();
            updateProgress(65f);

            createHtmlFromXsl();

            updateProgress(90f);

            copyExtraFiles();

            updateProgress(100f);

            try
            {
                File.Delete(projSettings.DestinationPath + projSettings.DocXmlFileName);
            }
            catch (Exception)
            {
            }

            string errorLogFil = projSettings.DestinationPath + "/errors.log";

            string endMessage = "The build is now finished";
            if (systemSvar != "")
            {
                string todoMessage = "";
                Assembly asm = Assembly.GetExecutingAssembly();
                string version = asm.GetName().Version.ToString();
                if (latestVersion != version)
                {
                    todoMessage = "\n\rTry upgrade to the latest version. This might solve the problem.";
                }
                StreamWriter skrivObjekt = new StreamWriter(errorLogFil, false, System.Text.Encoding.UTF8);
                skrivObjekt.Write(systemSvar + "\r\n" + todoMessage + "\r\nYour version: " + version);
                skrivObjekt.Close();

                MessageBoxResult svar = MessageBox.Show(endMessage + "\n\nDuring the build a number of errors occured." + todoMessage + "\nYou can find details in the error.log file.\n\nDo you want to se the log file?", "Build finished, but with errors", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (svar == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(errorLogFil);
                }



            }
            else if (!showAfterBuildCB.IsChecked.Value) MessageBox.Show(endMessage, "Build finished", MessageBoxButton.OK, MessageBoxImage.Asterisk);

            if (systemSvar == "" && File.Exists(errorLogFil))
            {
                File.Delete(errorLogFil);
            }

            updateProgress(0f);

            BuildButton.IsEnabled = true;

            if (showAfterBuildCB.IsChecked.Value)
            {
                string resultDoc = projSettings.DestinationPath + projSettings.DocHtmlFileName;
                System.Diagnostics.Process.Start(resultDoc);
            }

        }

        ///<summary>
        /// Make the list of as files
        ///</summary
        private void BuildDocumentationXml()
        {

            DataTypeUtil allDataTypes = DataTypeUtil.Instance;
            XmlDeclaration dec = allDocXml.CreateXmlDeclaration("1.0", null, null);
            allDocXml.AppendChild(dec);

            XmlElement contentNode = allDocXml.CreateElement("docElements");
            allDocXml.AppendChild(contentNode);

            XmlElement pathNode = allDocXml.CreateElement("basePath");
            pathNode.InnerText = "file:/" + xmlPath.Text.Replace("\\", "/") + "/";
            contentNode.AppendChild(pathNode);

            XmlElement newNode1 = allDocXml.CreateElement("introHeader");
            newNode1.InnerText = introHeader.Text;
            contentNode.AppendChild(newNode1);


            XmlElement newNode2 = allDocXml.CreateElement("introText");
            contentNode.AppendChild(newNode2);
            XmlCDataSection CData = allDocXml.CreateCDataSection(introText.Text.Replace("\r\n", "<br/>\r\n"));
            newNode2.AppendChild(CData);

            XmlElement createdNode = allDocXml.CreateElement("created");
            createdNode.InnerText = String.Format("{0:d/M yyyy}", DateTime.Now);
            contentNode.AppendChild(createdNode);


            XmlElement languageNode = allDocXml.CreateElement("language");
            switch ((CodeLanguage)projSettings.Language)
            {
                case CodeLanguage.Actionscript:
                    asDocumentation = new ASDocumentationBuilder();
                    languageNode.InnerText = "AS";
                    break;
                case CodeLanguage.Javascript:
                    asDocumentation = new JSDocumentationBuilder();
                    languageNode.InnerText = "JS";
                    break;
            }
            contentNode.AppendChild(languageNode);



            //loop trough all files
            int incFiles = 0;
            foreach (string filNavn in projSettings.AllFiles)
            {
                if (File.Exists(filNavn))
                {
                    incFiles++;
                    updateProgress((incFiles / projSettings.AllFiles.Count) * 38 + 2);
                    DateTime modifiedTime = File.GetLastWriteTime(filNavn);

                    XmlNodeList classXml = asDocumentation.AddFile(File.ReadAllLines(filNavn, Encoding.Default), modifiedTime, filNavn);

                    try
                    {
                        if (classXml.Count > 0)
                        {
                            foreach (XmlNode classNode in classXml)
                            {
                                if (classNode.InnerXml != "")
                                {
                                    allDataTypes.AddDataType(classNode.SelectSingleNode("name").InnerText, classNode.SelectSingleNode("package").InnerText, null);//,classNode.SelectSingleNode("fid").InnerText

                                    contentNode.AppendChild(allDocXml.ImportNode(classNode, true));
                                }
                                else
                                {
                                    systemSvar += "Empty class in: " + filNavn + "\r\n";
                                }
                            }
                        }
                        else
                        {
                            systemSvar += "File not added (no content): " + filNavn + "\r\n\r\n";
                        }
                    }
                    catch (Exception e)
                    {
                        systemSvar += "Exception error in: " + filNavn + "\r\n" + e.ToString() + "\r\n\r\n";
                    }

                    if (asDocumentation.SystemSvar != "")
                    {
                        systemSvar += "Error in " + filNavn + ":\r\n" + asDocumentation.SystemSvar + "\r\n\r\n";
                        asDocumentation.SystemSvar = "";
                    }
                }
            }

        }

        ///<summary>
        /// Make the list of as files
        ///</summary
        private void SaveXml()
        {
            XmlRestructure docXmlRestructure = new XmlRestructure(allDocXml);

            docXmlRestructure.UpdateSetterGetters();
            updateProgress(45f);

            docXmlRestructure.CreateInheritedElements();
            updateProgress(50f);

            docXmlRestructure.CreateNestedPackages();
            updateProgress(50f);

            docXmlRestructure.CreateClassIndex();
            updateProgress(57f);

            docXmlRestructure.UpdateTypesFullpath();
            updateProgress(61f);


            systemSvar += docXmlRestructure.Errors;

            try
            {
                StreamWriter skrivObjekt = new StreamWriter(projSettings.DestinationPath + projSettings.DocXmlFileName, false, System.Text.Encoding.UTF8);
                skrivObjekt.Write(docXmlRestructure.DocumentationXml.OuterXml);
                skrivObjekt.Close();
            }
            catch (Exception)
            {
                systemSvar += "\r\nCouldn't write XML file";
            }

            updateProgress(65f);
        }


        void copyExtraFiles()
        {
            string destPath = projSettings.DestinationPath + "/" + projSettings.StyleName;
            string srcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/styles/" + projSettings.StyleName;
            if (Directory.Exists(srcPath)) copyDirectory(srcPath, destPath);
        }

        void copyDirectory(string Src, string Dst)
        {
            String[] Files;

            if (Dst[Dst.Length - 1] != Path.DirectorySeparatorChar)
                Dst += Path.DirectorySeparatorChar;
            if (!Directory.Exists(Dst)) Directory.CreateDirectory(Dst);
            Files = Directory.GetFileSystemEntries(Src);
            foreach (string Element in Files)
            {
                // Sub directories
                if (Directory.Exists(Element) && Element.IndexOf(".svn") == -1)
                    copyDirectory(Element, Dst + Path.GetFileName(Element));
                // Files in directory
                else
                    try
                    {
                        File.Copy(Element, Dst + Path.GetFileName(Element), true);
                    }
                    catch (Exception) { }
            }
        }


        void createHtmlFromXsl()
        {
            string resultDoc = projSettings.DestinationPath + projSettings.DocHtmlFileName;
            if (projSettings.StyleName == "OrteliusXml") resultDoc = projSettings.DestinationPath + "/orteliusXml.xml";
            string xmlDoc = projSettings.DestinationPath + projSettings.DocXmlFileName;
            string xslDoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/styles/" + projSettings.StyleName + ".xsl";

            try
            {
                Processor processor = new Processor();
                System.IO.StreamReader reader = new System.IO.StreamReader(xmlDoc, System.Text.Encoding.UTF8);
                System.IO.TextWriter stringWriter = new System.IO.StringWriter();

                stringWriter.Write(reader.ReadToEnd());
                stringWriter.Close();

                reader.Close();

                System.IO.TextReader stringReader = new System.IO.StringReader(stringWriter.ToString());
                System.Xml.XmlTextReader reader2 = new System.Xml.XmlTextReader(stringReader);
                reader2.XmlResolver = null;

                // Load the source document
                XdmNode input = processor.NewDocumentBuilder().Build(reader2);

                // Create a transformer for the stylesheet.
                XsltTransformer transformer = processor.NewXsltCompiler().Compile(new System.Uri(xslDoc)).Load();
                transformer.InputXmlResolver = null;

                // Set the root node of the source document to be the initial context node				
                transformer.InitialContextNode = input;

                // Create a serializer				
                Serializer serializer = new Serializer();

                serializer.SetOutputFile(resultDoc);

                // Transform the source XML to System.out.				
                transformer.Run(serializer);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                systemSvar += "Error in xslt rendering:\r\n" + e.ToString();
            }

        }


        #endregion


        #region Save and load settings and projects

        void ChooseDestinationClick(object sender, EventArgs e)
        {

            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (xmlPath.Text != "" && xmlPath.Text != null) folderDialog.SelectedPath = xmlPath.Text;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (folderDialog.SelectedPath != null)
                {
                    changeFlag = true;
                    projSettings.DestinationPath = folderDialog.SelectedPath;
                    xmlPath.Text = projSettings.DestinationPath;
                }
            }

        }



        void SaveProjectClick(object sender, EventArgs e)
        {
            saveProject(false);
        }


        void SaveProjectAsClick(object sender, EventArgs e)
        {
            saveProject(true);
        }

        void saveProject(bool saveAsFlag)
        {

            changeFlag = false;
            if (saveAsFlag || appSettings.CurrentProject == "")
            {
                SaveFileDialog fileDialog = new SaveFileDialog();


                if (appSettings.CurrentProject != "")
                {
                    fileDialog.FileName = appSettings.CurrentProject;
                }
                else fileDialog.InitialDirectory = "Desktop";
                fileDialog.Filter = "Ortelius project files(*.orp)|*.orp";
                //fileDialog.FilterIndex = 1 ;
                fileDialog.Title = "Ortelius file";

                if (fileDialog.ShowDialog().Value)
                {
                    if (fileDialog.FileName != null)
                    {
                        appSettings.CurrentProject = fileDialog.FileName;
                        saveProjectSettings();

                    }

                }
            }
            else
            {
                saveProjectSettings();
            }
        }


        ///<summary>
        ///
        ///</summary>
        void saveProjectSettings()
        {
            if (appSettings.CurrentProject == "") return;
            try
            {
                using (TextWriter writer = new StreamWriter(appSettings.CurrentProject))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(ProjectSettings));
                    ser.Serialize(writer, projSettings);
                    writer.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Project couldn't be saved at: " + appSettings.CurrentProject, "Project couldn't be saved");
            }
        }


        void OpenProjectClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = "Desktop";
            openFileDialog1.Filter = "Ortelius project files(*.orp)|*.orp";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "Project file";
            if (openFileDialog1.ShowDialog().Value)
            {
                if (openFileDialog1.FileName != null)
                {
                    appSettings.CurrentProject = openFileDialog1.FileName;
                    LoadProject();
                }
            }
        }

        void LoadProject(){
            loadProjectSettings();
            renderList();
            xmlPath.Text = projSettings.DestinationPath;
            int basicIndex = 0;
            int count = 0;

            foreach (string Element in StyleCB.Items)
            {
                if (Element == projSettings.StyleName) basicIndex = count;                
                count++;
            }

            StyleCB.SelectedIndex = basicIndex;
        }


        void loadProjectSettings()
        {
            if (File.Exists(appSettings.CurrentProject))
            {
                ProjectSettings tempSettings = projSettings; 
                
                try
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(ProjectSettings));
                    using (TextReader reader = new StreamReader(appSettings.CurrentProject))
                    {
                        projSettings = (ProjectSettings) deserializer.Deserialize(reader);
                        reader.Close();
                    }
                }
                catch (Exception)
                {
                    projSettings = tempSettings;
                    MessageBox.Show("The project file hasen't the right format. It might be from a earlier version of Ortelius.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                introHeader.Text = projSettings.IntroHeader;
                introText.Text = projSettings.IntroText;
                showAfterBuildCB.IsChecked = projSettings.ShowAfterBuild;
                changeFlag = false;
            }
        }




        void SaveSettings()
        {
            string settingFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Ortelius";

            //string settingFile = Path.GetDirectoryName(Application.ExecutablePath)+"/settings.ors";
            if (!Directory.Exists(settingFile))
            {
                Directory.CreateDirectory(settingFile);
            }

            settingFile += @"\settings.ors";
            FileStream myStream = new FileStream(settingFile, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryFormatter binSkriver = new BinaryFormatter();
            binSkriver.Serialize(myStream, appSettings);
            myStream.Close();
        }

        void LoadSettings()
        {
            string settingFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Ortelius\settings.ors";

            if (File.Exists(settingFile))
            {
                try
                {
                    FileStream myStream = new FileStream(settingFile, FileMode.Open, FileAccess.Read);
                    myStream.Seek(0, SeekOrigin.Begin);
                    BinaryFormatter binLaeser = new BinaryFormatter();
                    appSettings = (GenerelSettings)binLaeser.Deserialize(myStream);
                    myStream.Close();
                    LoadProject();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        void ExitClick(object sender, EventArgs e)
        {
            string saveWarning = "Do you want to save the current changes?";
            if (changeFlag)
            {
                MessageBoxResult saveAnswer = MessageBox.Show(saveWarning, "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (saveAnswer == MessageBoxResult.Cancel) return;
                else if (saveAnswer == MessageBoxResult.Yes)
                {
                    saveProject(false);
                }
            }

            this.Close();

        }

        void OrteliusClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveSettings();
            }
            catch (Exception) { }
        }


        void NewProjectClick(object sender, EventArgs e)
        {
            string saveWarning = "Do you want to continue without saving your changes?";
            if (changeFlag)
            {
                if (MessageBox.Show(saveWarning, "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            }

            projSettings.ResetProjectSettings();
            //if (sender == newJSProjectToolStripMenuItem) 
            projSettings.Language = (int)CodeLanguage.Javascript;

            xmlPath.Text = "";
            appSettings.CurrentProject = "";
            introText.Text = "";
            introHeader.Text = "";
            populateStyleCombo();
            renderList();
        }
        #endregion

        //remove class
        void RemoveClassClick(object sender, RoutedEventArgs  e)
        {
            doRemoveClass();
        }
        //remove class
        private void doRemoveClass()
        {
            foreach (string i in FileList.SelectedItems)
            {
                projSettings.RemoveFile(i);
            }

            renderList();
        }

        //add folder
        void AddFolderClick(object sender, RoutedEventArgs  e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (projSettings.LastFolderName == "") folderDialog.SelectedPath = "Desktop";
            else folderDialog.SelectedPath = projSettings.LastFolderName;

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (folderDialog.SelectedPath != null)
                {
                    changeFlag = true;
                    projSettings.LastFolderName = folderDialog.SelectedPath;
                    addFolderFiles(folderDialog.SelectedPath);
                }
            }

            renderList();
        }

        void addFolderFiles(string asPath){
            String[] Files = Directory.GetFileSystemEntries(asPath);
            foreach (string Element in Files)
            {
                if (Directory.Exists(Element)) addFolderFiles(Element);
                else
                {
                    if (projSettings.AddFile(Element)) changeFlag = true;
                }
            }
        }

        //load
        private void MainFormLoad(object sender, RoutedEventArgs e)
        {
            //if Ortelius has been opened by an assoiacted file (.orp) then open the content of this file
            //or if it has been opened with commandline arguments (e.g. from flashdevelop)
            string[] args = Environment.GetCommandLineArgs();

            string paramName = "";
            string delimeter = "";
            bool loadSettingsTest = true;

            foreach (string param in args)
            {
                if (param.IndexOf("/folder:") == 0 || param.IndexOf("/name:") == 0 || param.IndexOf("/destination:") == 0) paramName = "";
                //MessageBox.Show(paramName +" - "+param);
                if (param.IndexOf(".orp") != -1 && param.IndexOf(".orp") == param.Length - 4)
                {
                    //	MessageBox.Show("Orp: "+param);
                    appSettings.CurrentProject = param;
                    LoadProject();
                    loadSettingsTest = false;
                }
                else if (param.IndexOf("/folder:") == 0 || paramName == "/folder:")
                {
                    //					MessageBox.Show("Folder: "+param);
                    delimeter = (paramName == "/folder:") ? " " : "";
                    startPath += delimeter + param.Replace("/folder:", "");
                    paramName = "/folder:";
                }
                else if (param.IndexOf("/name:") == 0 || paramName == "/name:")
                {
                    //					MessageBox.Show("Name: "+param);						
                    delimeter = (paramName == "/name:") ? " " : "";
                    startName += delimeter + param.Replace("/name:", "");
                    paramName = "/name:";
                }
                else if (param.IndexOf("/destination:") == 0 || paramName == "/destination:")
                {
                    //					MessageBox.Show("Destination: "+param);					
                    delimeter = (paramName == "/destination:") ? " " : "";
                    destinationPath += delimeter + param.Replace("/destination:", "");
                    paramName = "/destination:";
                }
                else
                {
                    //					MessageBox.Show("Nothing: "+param);
                    paramName = "";
                }


            }

            if (startPath != "")
            {
                loadSettingsTest = false;

                if (destinationPath == "") destinationPath = startPath + "\\documentation";
                projSettings.DestinationPath = destinationPath;
                xmlPath.Text = projSettings.DestinationPath;
                projSettings.LastFolderName = startPath;


                if (!Directory.Exists(projSettings.DestinationPath))
                {
                    Directory.CreateDirectory(projSettings.DestinationPath);
                    appSettings.CurrentProject = projSettings.DestinationPath + "/documentation.orp";
                    changeFlag = true;
                }
                else
                {
                    //look for orp file
                    String[] Files = Directory.GetFileSystemEntries(projSettings.DestinationPath);
                    string orpFile = "";
                    foreach (string Element in Files)
                    {
                        //		MessageBox.Show("Element: "+Element);
                        if (Element.IndexOf(".orp") != -1)
                        {
                            orpFile = Element;

                        }
                    }
                    if (orpFile == "")
                    {
                        appSettings.CurrentProject = projSettings.DestinationPath + "/documentation.orp";
                        changeFlag = true;

                    }
                    else
                    {
                        appSettings.CurrentProject = orpFile;
                        LoadProject();
                    }

                }


                addFolderFiles(startPath);
                renderList();

            }

            if (startName != null && (introHeader.Text == null || introHeader.Text == ""))
            {
                introHeader.Text = startName;
                projSettings.IntroHeader = introHeader.Text;
                changeFlag = true;
            }

            if (loadSettingsTest) LoadSettings();

            if (changeFlag)
            {

                if (appSettings.CurrentProject != "") saveProjectSettings();
                changeFlag = false;
            }
        }


        void MinimizeForm(object sender, EventArgs e)
        {
            
        }


        //private Point mouseOffset;
        //private bool isMouseDown = false;

        //private void frm_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        //{
        //    int xOffset;
        //    int yOffset;
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        xOffset = Location.X - Control.MousePosition.X;//-e.X - SystemInformation.FrameBorderSize.Width;
        //        yOffset = Location.Y - Control.MousePosition.Y;//e.Y ;

        //        mouseOffset = new Point(xOffset, yOffset);
        //        isMouseDown = true;
        //    }
        //}

        //private void frm_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        //{
        //    if (isMouseDown)
        //    {
        //        Point mousePos = Control.MousePosition;
        //        mousePos.Offset(mouseOffset.X, mouseOffset.Y);
        //        Location = mousePos;
        //    }
        //}

        //private void FrmGlavna_MouseUp(object sender,
        //System.Windows.Forms.MouseEventArgs e)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        isMouseDown = false;
        //    }
        //}



        void PictureBox3Click(object sender, EventArgs e)
        {
            openSite();
        }

        private void openSite()
        {
            string target = "http://ortelius.marten.dk";
            try
            {
                System.Diagnostics.Process.Start(target);
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }


        private void IntroTextChanged(object sender, TextChangedEventArgs e)
        {
            changeFlag = true;
            projSettings.IntroText = introText.Text;
        }

        void IntroHeaderChanged(object sender, TextChangedEventArgs e)
        {
            changeFlag = true;
            projSettings.IntroHeader = introHeader.Text;

        }

        void ShowAfterBuildChanged(object sender, RoutedEventArgs e)
        {
            changeFlag = true;
            projSettings.ShowAfterBuild = showAfterBuildCB.IsChecked.Value;

        }

        void GoToDocumentationClick(object sender, RoutedEventArgs e)
        {
            string resultDoc = projSettings.DestinationPath + projSettings.DocHtmlFileName;
            System.Diagnostics.Process.Start(resultDoc);
        }



        void NewVersionClick(object sender, RoutedEventArgs e)
        {
            openSite();
        }

        void MainFormShown(object sender, RoutedEventArgs e)
        {
            checkForUpdates();
        }



        void DonateClick(object sender, RoutedEventArgs e)
        {
            string target = "http://ortelius.marten.dk/donate.aspx";
            try
            {
                System.Diagnostics.Process.Start(target);
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }




        void HelpToolStripMenuItemClick(object sender, EventArgs e)
        {
            string target = "http://ortelius.marten.dk/";

            switch ((CodeLanguage)projSettings.Language)
            {
                case CodeLanguage.Actionscript:
                    target = "http://ortelius.marten.dk/sider/as-documentation-generator_27.aspx";
                    break;
                case CodeLanguage.Javascript:
                    target = "http://ortelius.marten.dk/sider/javascript-documentation-generator_95.aspx";
                    break;
            }

            try
            {
                System.Diagnostics.Process.Start(target);
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void StyleCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            projSettings.StyleName = StyleCB.Items[StyleCB.SelectedIndex].ToString();
        }


      
    }
}
