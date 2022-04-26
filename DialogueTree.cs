using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Dialogue_Scripts
{

    public class DialogueTree
    {
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
        
        public DialogueNode root;
        private string[] _script;
        private int _currentScriptIndex = 0;
        public Dictionary<string, string[]> characterInfo;
        private ScriptType _scriptType;
        private static DialogueNode EoD = new DialogueNode("EoD","EoD");
        private string _fileType;
        // public List<string> 
        
        /*The filepath must be in the assets folder because idk...*/
        //todo: make a system so you can set it up in the Inspector in Unity
        //Like I'll ever get to that :(
        public DialogueTree(string filePath)
        {
            characterInfo = new Dictionary<string, string[]>(10);
            
            
        }

        public DialogueTree(UnityEngine.TextAsset file)
        {
            characterInfo = new Dictionary<string, string[]>(10);
            SetUpArray(file);                
           
        }

        private void SetUpArray(TextAsset file)
        {
            _script = file.ToString().Split('\n');
            RemoveComments();
            if (_script.Length < 1)
            {
                SetRootError("THERE ARE NO LINES IN THE FILE/");
                return;
            }
            

            string typeLine = _script[_currentScriptIndex++];//go to next line since we won't be reading this again
            if (typeLine.Length < 4 || typeLine.Substring(0, 4) != "TYPE")
            {
                SetRootError("FIRST LINE IS NOT A SCRIPT TYPE DEFINITION.");//shit code but oh well
                return;
            }

            // int lastSpace = typeLine.LastIndexOf(' ');
            int startIndex = typeLine.IndexOf('=');
            string typeName = typeLine.Substring(startIndex,typeLine.Length-startIndex);
            typeName = new string(typeName.Where(c => !char.IsWhiteSpace(c)).ToArray());
            typeName = typeName.Replace("=", "");//is this necessary? todo
            if(!stringToScriptType.ContainsKey(typeName))
            {
                SetRootError("NOT A VALID SCRIPT TYPE.");
                return;
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
            
        }

        private void SetRootError(string messsage)
        {
            root = new DialogueNode("ERROR", messsage);
        }

        //technically removes blank lines too... shhhhhhhh
        private void RemoveComments()
        {
            _script = _script.Where(x => x[0] == '#'||x.Length<1).ToArray();
        }
        
    }
    
    /*The general dialogue line. This is for lines that lead to another line (as opposed to a choice)*/
    public class DialogueNode : NodeType
    {
        //todo: add option for 'animation' change
        public DialogueNode():base(){}
        public DialogueNode(string name, string line):base(name,line){}
    }

    /*The line before a choice appears. This is for lines that lead to a choice selection for the player.*/
    public class ChoiceNode : NodeType
    {
        public List<SelectNode> choices;

        public ChoiceNode() : base()
        {
            choices = new List<SelectNode>(2);
        }

        public ChoiceNode(string name, string line) : base(name, line)
        {
            choices = new List<SelectNode>(2);
        }
        
        
    }

    /*The node that contains a player selection*/
    public class SelectNode : NodeType
    {
        public bool canDisappear, seen;

        public SelectNode() : base()
        {
            seen = false;
        }

        public SelectNode(string line, bool canDisappear = false)
        {
            name = "NONE";
            this.line = line;
            this.canDisappear = canDisappear;
            seen = false;
        }
    }
    
    public abstract class NodeType
    {
        protected string name, line;
        protected NodeType nextLine;

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