using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dialogue_Scripts
{
   [Obsolete("Remaking this so it's an abstract class, so please wait!")] 
    public class DialogueNode
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
        
        public DialogueNode(string name, string message,List<int> connection, bool hasChoice = false)
        {
            _name = name;
            _connection = connection ?? new List<int>{-1};
            _message = message;
            _connectionsAreChoices = hasChoice || _connection.Count>1;
            ChoiceHasBeenViewed = false;
        }

        private static readonly DialogueNode END = new DialogueNode("END","END",null);
        private static readonly DialogueNode PAUSE = new DialogueNode("PAUSE","PAUSE",null);
        //message conventions are Linenumber{Name}Message[Connection,Connection,Connection...]isChoicehasChoice
        //isChoice and hasChoice are either t for true or any other character for false
        //Connections are ints
        //Linenumber is int
        //end the dialogue with Linenumber--END--
        public static Dictionary<int, DialogueNode> CreateDialogueNodes(TextAsset textFile)
        {
            Dictionary<int,DialogueNode> nodes = new Dictionary<int, DialogueNode>();
    //todo: fix minimap saving
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
                            new DialogueNode
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
                            new DialogueNode(
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