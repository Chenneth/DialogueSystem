using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Dialogue_Scripts
{
    
    public class DialogueTree
    {
        public const bool descriptiveDebug = true;
        private enum ScriptType
        {
            playwright = 0,
            //todo: fill this out with different formats
        }

        private static Dictionary<string, ScriptType> stringToScriptType = new Dictionary<string, ScriptType>
            {
                {"PLAYWRIGHT", ScriptType.playwright},
            }
            ;

        public Dictionary<string, RootNode> roots;
        private string[] _script;
        private int _currentScriptIndex = 0;
        //key: character name
        //value: character description, character filepath
        public Dictionary<string, string[]> charactersInformation;
        private ScriptType _scriptType;
        private static DialogueNode EoD = new DialogueNode("EoD","EoD");
        private string _fileType;

        public static string DefaultDescription = "";
        // public List<string> 
        
        /*The filepath must be in the assets folder because idk...*/
        //todo: make a system so you can set it up in the Inspector in Unity
        //Like I'll ever get to that :(
        //todo: decide how to get this file (I am incredibly lazy)
        public DialogueTree(string filePath)
        {
            charactersInformation = new Dictionary<string, string[]>(10);
            roots = new Dictionary<string, RootNode>(3);
            
        }

        public DialogueTree(UnityEngine.TextAsset file)
        {
            charactersInformation = new Dictionary<string, string[]>(10);
            roots = new Dictionary<string, RootNode>(3);
            SetUpArray(file);                
            
        }

        private void SetUpArray(TextAsset file)
        {
            //splits the file into strings delimited by the newline character, with all lines starting with a "#" and blank lines removed.
            
            // _script = file.ToString().Split('\n').Where(x=>x[0]!='#'||x.Length==0).ToArray();
            //todo: test this regex removal
            _script = Regex.Replace(file.ToString(), @"#.*[\n]","\n").Split('\n');
            //I have no idea if this properly replaces the things (it should though)
            if(descriptiveDebug)
            {
                Debug.Log($"Script length is {_script.Length}.");
                foreach (string line in _script)
                {
                    Debug.Log(line);
                }
            }
            
            
            if (_script.Length < 1)
            {
                throw new UnityException("There are no lines in the file.");
            }
            

            string typeLine = _script[_currentScriptIndex++];//go to next line since we won't be reading this again
            if (typeLine.Length < 4 || typeLine.Substring(0, 4) != "TYPE")
            {
                throw new UnityException($"First line must be the type of script this script is. Line was\n" +
                                         $"{typeLine}");
                //i guess i could make it recognize the script type... if I actually took language processing courses that is :(
            }

            // int lastSpace = typeLine.LastIndexOf(' ');
            int startIndex = typeLine.IndexOf('=');
            string typeName = typeLine.Substring(startIndex,typeLine.Length-startIndex);
            typeName = new string(typeName.Where(c => !char.IsWhiteSpace(c)).ToArray());
            typeName = typeName.Replace("=", "");//is this necessary? todo
            if(!stringToScriptType.ContainsKey(typeName))
            {
                throw new UnityException($"Not a valid script type. Found {typeName}");
            }
            
            _scriptType = stringToScriptType[typeName];
            string imageType = _script[_currentScriptIndex];
            if (imageType.Length < 21 || imageType.Substring(0,19)!="EXPRESSION_FILETYPE")
                _fileType = ".jpg";//jpg ftw jpg artifactingllololololo
            else
            {
                startIndex = imageType.IndexOf('=');
                _fileType = imageType.Substring(startIndex);
                _fileType = _fileType.Replace(" ", "");
                _fileType = _fileType.Replace("=", "");//todo same as line 71
                _currentScriptIndex++;
            }
            //begin parsing file
            switch (_scriptType)
            {
                case ScriptType.playwright:
                    MakeTreePlaywright();
                    break;
            }
        }
        
        private void MakeTreePlaywright()
        {
            string curLine = _script[_currentScriptIndex++];
            if(!curLine.StartsWith(":START ")||!curLine.EndsWith("'Characters in the Play':"))
            {
                throw new UnityException(
                    $"First line after the image type or script type must be the 'Characters in the Play' header. Found {curLine}");
            }
            PlayWrightFindCharacters(ref curLine);
            if(!curLine.StartsWith(":START "))
                throw new UnityException($"Script error. Expected line {_currentScriptIndex} to start with " +
                                         $"\":Start\" but found {curLine} instead.");
            Match match = Regex.Match(curLine, @"['](.*?)[']");
            if(match.Success)
                AddScene(match.Groups[0].ToString());
            else
            {
                throw new UnityException("I like throwing these exceptions instead of writing to the console.");
            }
            
        }

        //_curScriptIndex will be set to the next unread line after this function ends :)
        private void PlayWrightFindCharacters(ref string curLine)
        {
            curLine = _script[_currentScriptIndex++];
            while (!curLine.Equals(":END 'Characters in the Play'"))
            {
                string[] charInfo = curLine.Split(",");
                if (descriptiveDebug)
                {
                    Debug.Log($"Item split into {charInfo.Length} elements. Item is {curLine}. Outputting each element.");
                    foreach (string str in charInfo)
                    {
                        Debug.Log(str);
                    }
                }

                string description = String.IsNullOrWhiteSpace(charInfo[1]) || String.IsNullOrEmpty(charInfo[1])
                    ? DefaultDescription
                    : charInfo[1];
                string filePath;
                if (charInfo.Length < 3) //if no filepath was given
                {
                    //todo: search for assetbundle (i'm too lazy to do this rn) 
                    filePath = "";
                }
                else //filepath was given and we can set filePath to it
                {
                    filePath = charInfo[2];
                }
                
                //add the character's stuff into the thingy ma-bob
                //todo: this doesnt work with assetbundles probably because the bundle itself should be in the filepath
                charactersInformation.Add(
                    charInfo[0],new string[]{
                    description,
                    filePath
                });
                curLine = _script[_currentScriptIndex++];
            }
        }
        /*private void SetRootError(string messsage)
        {
            roots.Add("START_SCENE", new DialogueNode("ERROR",messsage));
        }*/

        //function is recursive for any sub-sections users decide to input (much to my dismay)
        private void AddScene(string sceneName)
        {
            if(descriptiveDebug)
                Debug.Log("Creating scene titled " + sceneName);
            //first line of the scene should be a character name
            string curLine = _script[_currentScriptIndex++];
            if(descriptiveDebug)
                Debug.Log($"First character name is " + curLine);
            string name, line, expression;
            NodeType curNode = new RootNode();
            roots.Add(sceneName,(RootNode)curNode);
            /*Would it be better to have this be an equals? I wanted it to end the check sooner so as to not hog too much resources.
             * The question is, "Is there an instance where :END will not refer to the current section?"
             * I do not think so, due to the recursive nature of this program.
             */
            while /*(!curLine.StartsWith(":END")*/true)
            {
                //todo: just check the loop condition at the end after updating curLine so we can also check for new scene starting
            }
        }
    }
    
    /*The general dialogue line. This is for lines that lead to another line (as opposed to a choice)*/
    public class DialogueNode : NodeType
    {
        //todo: add option for 'animation' change
        //what did I mean by 'animation'? sprite expression?
        private string _expression;
        public readonly bool ExpressionUpdate;
        public string Expression
        {
            get => _expression;
        }

        public DialogueNode():base(){_isRoot = false;}

        public DialogueNode(string name, string line, string expression = "") : base(name, line)
        {
            _expression = expression;
            _isRoot = false;
            ExpressionUpdate = String.IsNullOrEmpty(_expression) || String.IsNullOrWhiteSpace(_expression);
        }
    }

    /*The line before a choice appears. This is for lines that lead to a choice selection for the player.*/
    public class ChoiceNode : NodeType
    {
        public List<SelectNode> choices;

        public ChoiceNode() : base()
        {
            choices = new List<SelectNode>(2);
            _isRoot = false;
        }

        public ChoiceNode(string name, string line) : base(name, line)
        {
            choices = new List<SelectNode>(2);
            _isRoot = false;
        }
        
        
    }

    /*The node that contains a player selection*/
    public class SelectNode : NodeType
    {
        public bool canDisappear, seen;

        public SelectNode() : base()
        {
            _isRoot = false;
            seen = false;
        }

        public SelectNode(string line, bool canDisappear = false)
        {
            name = "NONE";
            this.line = line;
            this.canDisappear = canDisappear;
            _isRoot = false;
            seen = false;
        }
    }

    public class RootNode : NodeType
    {
        public RootNode() : base("Root", "This is the root node")
        {
            _isRoot = true;
        }
    }
    
    public abstract class NodeType
    {
        protected string name, line;
        protected NodeType nextLine;
        protected bool _isRoot;
        public bool IsRoot { get; }

        //getter and setter, prevents the value from being overriden once set (it shouldn't ever be overriden anyways)
        public NodeType NextLine
        {
            get => nextLine;
            //todo: is this passing by reference?
            set => nextLine ??= value;
        }


        protected NodeType()
        {
            name = line = "NONE";
        }
        
        protected NodeType(string name, string line)
        {
            this.name = name;
            this.line = line;
        }
    }
    
   [Obsolete("Please use DialogueTree instead.")] 
    public class DialogueNode_OUTDATED
    {
        /*public static readonly Regex connections = new Regex("[[][\\d](,\\d)+?[]]");*/
        
        //Start of Properties for object
        public string Name => _name;
        public List<int> Connection => _connection;

        public string Message => _message;

       
        public bool ConnectionsAreChoices => _connectionsAreChoices;
        //End of Properties for object
        
        //Start of variables for object
        //the line(s) that follow after this message
        private List<int> _connection;
        
        
        private string _message;
        
        //is this dialogue a choice option for the player
        /*private bool _isChoice;*/

        //are the associated dialogue values with _connection choices
        private bool _connectionsAreChoices;
        
        //who says the line
        private readonly string _name;
        //End of variables for object

        public bool ChoiceHasBeenViewed
        {
            get => _choiceHasBeenViewed;
            set => _choiceHasBeenViewed = value;
        }
        private bool _choiceHasBeenViewed;
        
        public DialogueNode_OUTDATED(string name, string message,List<int> connection, bool hasChoice = false)
        {
            _name = name;
            _connection = connection ?? new List<int>{-1};
            _message = message;
            _connectionsAreChoices = hasChoice || _connection.Count>1;
            ChoiceHasBeenViewed = false;
        }

        private static readonly DialogueNode_OUTDATED END = new DialogueNode_OUTDATED("END","END",null);
        private static readonly DialogueNode_OUTDATED PAUSE = new DialogueNode_OUTDATED("PAUSE","PAUSE",null);
        //message conventions are Linenumber{Name}Message[Connection,Connection,Connection...]isChoicehasChoice
        //isChoice and hasChoice are either t for true or any other character for false
        //Connections are ints
        //Linenumber is int
        //end the dialogue with Linenumber--END--
        public static Dictionary<int, DialogueNode_OUTDATED> CreateDialogueNodes(TextAsset textFile)
        {
            Dictionary<int,DialogueNode_OUTDATED> nodes = new Dictionary<int, DialogueNode_OUTDATED>();
    
            //places each line into a string array based on the each newline
            string[] strings = textFile.ToString().Split('\n');
            int i = 0;
            foreach (var temp in strings)
            {
                int tempLength = temp.Length;
                int key;
                i++;
                
                if(temp.Contains("--END--"))
                {
                    int.TryParse(temp.Substring(0, temp.IndexOf('-')), out key);
                    nodes.Add(key,END);
                    break;
                }
                if (temp.Contains("--PAUSE--"))
                {
                   if( int.TryParse(temp.Substring(0, temp.IndexOf('-')), out key))
                       nodes.Add(key,PAUSE);
                   else
                   {
                       if(!nodes.ContainsKey(i))
                            nodes.Add(i,PAUSE);
                       else
                       {
                       }
                   }
                   continue;
                }
                
                if(int.TryParse(temp.Substring(0,temp.IndexOf('{')),out key))
                {
                    //these lines grab the numbers within the [] and throws them in a list of ints
                    int indexStart = temp.IndexOf('[');
                    int indexEnd = temp.IndexOf(']');
                    string connectionsRaw = temp.Substring(indexStart, temp.IndexOf(']') - indexStart+1);
                    
                    //checks if this is a multi list (there are multiple connections)
                    //if it's not adds the single number (connection)
                    string[] connections =Enumerable.Contains(connectionsRaw, ',')? connectionsRaw.Split(','):new string[]{connectionsRaw.Substring(1,connectionsRaw.IndexOf(']')-1)};
                    
                    List<int> connectionsList = /*connections.ConvertAll(c=>(int) char.GetNumericValue(c))*/
                    new List<int>(connections.Length);
                    /*
                    connectionsList.RemoveAll(x => x == -1);
                    */
                    for (var index = 0; index < connections.Length; index++)
                    {
                        var connection = new String(connections[index].Where(char.IsDigit).ToArray());
                        int.TryParse(connection, out var egg);
                        connectionsList.Add(egg);
                    }

                    //removes any connection that isn't a valid line number
                    connectionsList.RemoveAll(x => x < 1);
                                    //all this if statement does is make it do one less substring thing so idk if it's needed
                    if (temp.IndexOf(']') != tempLength)
                    {
                        /*nodes.Add(key,
                         new DialogueNode(
                         temp.Substring(temp.IndexOf('\{')),temp.Substring(1, temp.IndexOf('[')-1), 
                            connectionsList,
                            temp.Substring(indexEnd+1,1).Equals("t"),temp.Substring(indexEnd+2,1).Equals("t") ));*/
                        var startIndexName = temp.IndexOf('{')+1;
                        var endIndexName = temp.IndexOf('}')+1;
                        var startIndexConnection = temp.IndexOf('[')+1;
                        nodes.Add
                        (
                            key,
                            new DialogueNode_OUTDATED
                            (
                                temp.Substring(startIndexName,endIndexName-startIndexName-1),
                                temp.Substring(endIndexName,startIndexConnection-1-endIndexName),
                                connectionsList,
                                temp.Substring(tempLength-2).StartsWith("t")
                            )
                        );
                        
                    }
                    else
                    {
                        var startIndexName = temp.IndexOf('{')+1;
                        var endIndexName = temp.IndexOf('}')+1;
                        var startIndexConnection = temp.IndexOf('[');
                        /*nodes.Add(key,new DialogueNode(temp.Substring(1, temp.IndexOf('[')-1), 
                            connectionsList));*/
                        nodes.Add(
                            key,
                            new DialogueNode_OUTDATED(
                                temp.Substring(startIndexName,endIndexName-startIndexName-1),
                                temp.Substring(endIndexName,startIndexConnection-endIndexName),
                                connectionsList));
                    }
                }
            }
            //todo: it's not grabbing anything past the first pause line

            return nodes;
        }

        public bool isEnd()
        {
            return this.Message.StartsWith(END.Message) && _name.StartsWith(END.Name);
        }
    }
}