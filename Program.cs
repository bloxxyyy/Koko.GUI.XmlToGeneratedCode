using Koko.RunTimeGui;
using Koko.XmlToGeneratedCode;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;

var projectPath = Directory.GetCurrentDirectory();

string Text = "";

/// <summary>
///  The file location where your xml files are located.
/// </summary>
var xmlFilesLocation = projectPath + "\\XML";

/// <summary>
///  The file location where your generated files are stored.
/// </summary>
var generatedFilesLocation = projectPath + "\\Generated";

var longName = "Koko.RunTimeGui, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null";
var asm = Assembly.Load(longName);

if (!Directory.Exists(xmlFilesLocation))
    throw new ArgumentException($"Path \"{xmlFilesLocation}\" does not exist, please check your current working directory");

var files = GetAllXmlFiles(xmlFilesLocation);

files.ForEach(xmlFileLocation => {
    var fileName = "Gen_" + System.IO.Path.GetFileNameWithoutExtension(xmlFileLocation).Replace(" ", "_");
    var generatedfilesLocation = generatedFilesLocation + "\\" + fileName + ".cs";
    CreateNewFile(generatedfilesLocation, fileName, xmlFileLocation);
});

/// <summary>
/// To check for all xml files in directory plus sub directories.
/// </summary>
/// <param name="targetDirectory"></param>
List<string> GetAllXmlFiles(string targetDirectory) {
    var files = new List<String>();
    foreach (string fileName in Directory.EnumerateFiles(targetDirectory, "*.xml", SearchOption.AllDirectories)) {
        files.Add(fileName);
    }
    return files;
}

void CreateNewFile(string GeneratedFileLocation, string filename, string xmlFileLocation) {
    try {
		using FileStream fs = File.Create(GeneratedFileLocation);
		GenerateFileInformation(fs, filename, xmlFileLocation);
	} catch (Exception ex) {
        Console.WriteLine(ex.ToString());
    }
}

void GenerateFileInformation(FileStream fs, string filename, string xmlPath) {
	XmlReaderSettings settings = new XmlReaderSettings {
		IgnoreWhitespace = true
	};

	using var fileStream = File.OpenText(xmlPath);
	using XmlReader reader = XmlReader.Create(fileStream, settings);

	var start = "using Microsoft.Xna.Framework;\nusing Koko.RunTimeGui;\nusing Koko.RunTimeGui.Gui.Initable_Components;\n\nnamespace Koko.Generated { \npublic class " + filename + " : IInitable { \npublic void Init() {\n";
	var end = "}\n}\n}\n";
	var writestart = new UTF8Encoding(true).GetBytes(start);
	var writeend = new UTF8Encoding(true).GetBytes(end);
	fs.Write(writestart, 0, writestart.Length);

	while (reader.Read()) {
		var line = "";

		switch (reader.NodeType) {
			case XmlNodeType.Element:
				try {
					Debug.WriteLine(reader.Name); // some weird bug sometimes where it cant be found.
					var instance = Activator.CreateInstance(asm.GetType("Koko.RunTimeGui." + reader.Name)) as IComponent;
					line += Elements(reader, instance);
				} catch (System.Exception) {
					throw;
				}
				break;

			case XmlNodeType.Text:
				Text = reader.Value;
				if (Text is null)
					Text = "";

				if (Text != "") {
					line += Text + "\";\n" +
						"component.AddChild(temp);\n";
				}

				break;

			case XmlNodeType.EndElement:
				if (Text == "") {
					line += "\";\n" +
						"component.AddChild(temp);\n";
				}

				try {
					var type = asm.GetType("Koko.RunTimeGui." + reader.Name);
					var instance = Activator.CreateInstance(type) as IComponent;

					line += EndTag(reader, instance);

				} catch (Exception) {
					Console.WriteLine("Couldn't find XML TYPE: " + reader.Name + " Create Component first!");
					throw;
				}

				break;

			default:
				Console.WriteLine($"Unknown: {reader.NodeType}");
				break;
		}

		var write = new UTF8Encoding(true).GetBytes(line);
		fs.Write(write, 0, write.Length);
	}

	fs.Write(writeend, 0, writeend.Length);
}

string Elements(XmlReader reader, IComponent componentType) {

    var setparentToGui = "IParent component = GUI.Gui;";
    var createTempComponent = "BaseComponent temp;";

    if (reader.Name == "GUI") // special case.
        return $"{setparentToGui}\n{createTempComponent}\n";

    var tagVal = "\"" + reader.GetAttribute("Tag") + "\"";
    var marginVal = Helper.GetIntergerValue(reader.GetAttribute("Margin"));
    var borderVal = Helper.GetIntergerValue(reader.GetAttribute("Border"));
    var isDraggableValue = Helper.GetBooleanAttribute(reader, "IsDraggable", false);

    var setnew = $"new {reader.Name}() {{ Parent = component";
    var tag = $"Tag = {tagVal}";
    var margin = $"MarginalSpace = new Margin({marginVal})";
    var border = $"BorderSpace = new Margin({borderVal})";
    var isDraggable = $"IsDraggable = {isDraggableValue.ToString().ToLower()}";

    //if (componentType is IChooseable<ISelectable>) return $"component = {setnew}, {tag} }};\n";

    if (componentType is IParent) {
        var backgroundColor = $"BackgroundColor = {Helper.GetBackgroundVal(reader)}";
        var columns = $"Columns = {Helper.GetColumnsVal(reader)}";

        if (componentType is GridPanel)
            return $"component = {setnew}, {tag}, {border}, {margin}, {backgroundColor}, {columns}, {isDraggable} }};\n";

        if (componentType is Nav)
            return $"component = {setnew}, {tag}, {border}, {margin}, {backgroundColor}, {isDraggable} }};\n";

        if (componentType is IParent)
            return $"component = {setnew}, {tag}, {border}, {margin}, {backgroundColor}, {isDraggable} }};\n";


        return $"component = {setnew}, {tag}, {border}, {margin}, {backgroundColor} }};\n";
    }

    return $"temp = {setnew}, {tag}, {border}, {margin} }};\n temp.Text = \"";

}

string EndTag(XmlReader reader, IComponent componentType) {
    if (reader.Name == "GUI")
        return "";

    var addComponentToParent = "((BaseComponent)component).Parent.AddChild((BaseComponent)component);";
    var setComponent = "component = ((BaseComponent)component).Parent;";

    //var checkIfNav "if (temp is IChooseable<ISelectable>)"

    //if (componentType is IChooseable<ISelectable>)
    //    return $"{addComponentToParent}\n{setComponent}\n";

    if (componentType is IParent) // besides GUI
        return $"{addComponentToParent}\n{setComponent}\n";

    return "";
}

Environment.Exit(0); // force exit
