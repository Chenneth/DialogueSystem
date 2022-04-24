//TODO: Shops should be opened via choices in dialogue (Buy vs sell choice buttons)
//TODO: EITHER DO THIS OR DIR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Dialogue_Scripts;


public delegate void DialogueStartedEvent();

public delegate void DialogueEndedEvent();

public delegate void DialoguePausedEvent();

public delegate void DialogueUnpausedEvent();

public class DialogueManager : MonoBehaviour
{
    
    [NonSerialized]
    public UnityEvent dialogueStartEvent;

    [NonSerialized]
    public UnityEvent dialogueEndEvent;
    
    [NonSerialized]
    public UnityEvent dialoguePauseEvent;
    
    [NonSerialized]
    public UnityEvent dialogueUnpauseEvent;

//    public event DialogueStartedEvent _dialogueStartedEvent;
    
    
   // public event DialogueEndedEvent _dialogueEndedEvent;
    
  
    private static DialogueManager _instance;

    public static DialogueManager Instance => _instance;
    
    //should i be sorting the variables by type name or actual var name?
    /***********************CHOICES VARIABLES*********************/
    /// <summary>
    /// Integer that defines the index of the actively selected choice button. Possible values start at 0 and increment by +1 for each button.
    /// </summary>
    private int _currentSelectedChoice;
    private bool _displayingChoices;


    private GameObject _choiceButton;
    /***********************END CHOICES VARIABLES*********************/
    
    /***********************DIALOGUE READING VARIABLES*********************/
    private bool _displayAll;
    //if the dialogue is activated? on? i can't think of a word
    private bool _dialogueOn;
    
    //if the dialogue is still going through an animation display?
    private bool _displayingDialogue;

    //if the line still has dialogue remaining to display (but was too large to display in one step)
    private bool _dialogueRemaining;

    public bool DisplayingDialogue => _displayingDialogue;

    //private char[] _chars;
    private Dictionary<int, DialogueNode> _dialogueTree;
    private IEnumerator _displayLine;
    private int _currentNode;    
    private int _lineIndex;

    /*
    private string _previousSpeaker;
    */
    private int _previousSpeakerIndex;
    
    private Dictionary<string, Sprite> _speakerImages;

    //i have no idea what this variable is for
    //private bool waitingToDisplay;
    
    /// <summary>
    /// Boolean that shows whether the History is displayed.
    /// True if History is displayed. False otherwise.
    /// </summary>
    private bool _historyDisplayed;

    //todo: convert entire access to either static or object oriented. Right now it's conflicting but shouldn't create issues since there should only be one instance of this script.
    /// <summary>
    /// Static access to determine if the dialogue is currently paused.
    /// </summary>
    public static bool paused;

    //todo: a PlayerPrefs that makes it completely skip the text animation (rn it just displays it extremely fast and moves to the next line without other input)
    /// <summary>
    /// Boolean which determines if the player wants to skip text.
    /// True if skipping, false otherwise.
    /// Will not skip if displaying choices.
    /// </summary>
    private bool skipText;

    
    /***********************END DIALOGUE READING VARIABLES*********************/

    
    /***********************UI VARIABLES*********************/
    public GameObject choiceBox;
    //a list containing the _choices buttons
    private List<GameObject> _choices;

    //a list containing the child object named Highlight (idk what else to call it | I'm too lazy to rename it)
    private List<Image> _choicesSelectionIndicator;
    private List<GameObject> _textChoices;
    
    public Text nameTextBox;
    public Image speakerImage;
    
    public Text textBox;
    private RectTransform _textBoxRectTransform;

   //the parent object that holds all the history gameobjects
    public GameObject historyParent;
    //i don't know why we can't just stuff and all this feels excessive
    public GameObject historyContent;

    public ScrollRect historyScrollRect;
    public GameObject scrollBar;
    
    public GameObject spriteContainer, dialogueContainer;

    private Sprite speakerImageDefault;

    //the image that appears to show the text can be continued or whatever I can't think rn
    public GameObject NextIndicator;


    private TextGenerator _textGenerator;

    private TextGenerationSettings _generationSettings;
   
    /***********************END UI VARIABLES*********************/


    /***********************PLAYER PREFS VARIABLES*********************/
    //are these necessary?
    private const string PrefsKeySpeed = "Display Speed";
    private int _displaySpeed;

    //run with the assumption that 1 is true for the pref (since player prefs can't have a bool lmao)
    private const string PrefsKeyAuto = "Auto Scroll on Click";
    private bool _autoScrollOnClick;
    
    //If true, the player does not need to hold the ctrl key to skip text
    private bool skipTextModeToggle;
    private const string PrefsKeySkipTextToggle = "Skip Text Toggle";
    /***********************END PLAYER PREFS VARIABLES*********************/

    /***********************HISTORY VARIABLES*********************/
    //no this does not include the ui vars. I'm not sure how to sort these tbh
    private GameObject _nameCardHistoryPrefab, _textHistoryPrefab, _nameWrapperPrefab, _textWrapperPrefab, _choiceWrapperPrefab;
    
    //literally contains everything that i'm throwing in the history
    private List<Text> _historyTextList;
    
    private int currentHistoryIndex;

    private Transform _currentTextWrapper;

    private bool _createdChoice;
    //and yeah, it might be better to do some other indenting magic than creating multiple text objects but I'm stoopid
    /***********************END HISTORY VARIABLES*********************/
    
    

    public static bool isDialogueFinished;

    private bool started =false;
    
    
    private void Start()
    {
        
        
    }

    void OnSceneChange()
    {
        
    }
    
    private void Update()
    {
        //dialogue script can be paused with a line that contains --PAUSE-- in it
        //this does not display anything; it will simply move to the next line in numerical order
        if (!paused)
        {
            //if the dialogue is on (since we don't need this displaying or whatever when we hide the box)
            if (_dialogueOn)
            {
                //if the player has the setting so they have to hold to skip text
                //and if they are holding a ctrl key
                if (!skipTextModeToggle)
                {
                    if ((Input.GetKey(KeyCode.LeftControl)) || Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.Equals))
                    {
                        skipText = true;
                        
                    }
                    else
                    {
                        skipText = false;
                    }
                }
                
                //if the user has it togglable, and they press one of the keys, it switches the skip mode
                else if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)||Input.GetKey(KeyCode.Equals))
                {
                    skipText = !skipText;
                }
                
                
                var dialogueNode = _dialogueTree[_currentNode];

                if (!_historyDisplayed)
                {
                    if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.H) ||
                        Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
                    {
                        DisplayHistory();
                        return;
                    }

                    if (_displayingChoices)
                    {
                        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
                        {
                            UpdateSelectedChoice(_currentSelectedChoice - 1);
                        }
                        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
                                 Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
                        {
                            UpdateSelectedChoice(_currentSelectedChoice + 1);
                        }

                        if ((Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.Space))&& _currentSelectedChoice>=0)
                        {
                            StoredMakeChoice();   
                        }
                    }
                    else
                    {
                        if (skipText && !_dialogueRemaining &&!_displayingDialogue && !_dialogueTree[_currentNode].ConnectionsAreChoices && !_displayingChoices)
                        {
                          
                            if (!NextLine(dialogueNode.Connection[0]))
                            {
                                EndDialogue();
                                return;
                            }
                        }
                        if (MoveDialogueForward())
                        {
                            //is the animation still playing? if so, then skip the animation completely
                            //(all this is is changing the variable that acts in the coroutine)
                            if (_displayingDialogue)
                            {
                                _displayAll = true;
                            }
                            else
                            {
                                if (!_dialogueRemaining &&!_dialogueTree[_currentNode].ConnectionsAreChoices && !_displayingChoices)
                                {
                                //because the dialogue has no choices, we can simply check its first connection
                                //if that connection returns false, that means the dialogue is over
                                
                                    if (!NextLine(dialogueNode.Connection[0]))
                                    {
                                        EndDialogue();
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    /*
                    else if(!_displayingDialogue)
                */
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.H) || ((historyScrollRect.verticalNormalizedPosition<=.05f||!scrollBar.activeSelf)&&Input.GetAxisRaw("Mouse ScrollWheel")<0f)
                        //|| todo: scroll closes history when history is at bottom
                        /*Input.GetAxisRaw("Mouse ScrollWheel") < 0f*/
                    )
                    {
                        CloseHistory();
                    }
                }
            }

            if (isDialogueFinished)
            {
                EndDialogue();
            }
        }
    }
    
    public void StartDialogue(TextAsset textFile)
    {
        paused = false;
        dialogueStartEvent.Invoke();
        _historyDisplayed = false;

        _dialogueOn = true;
        //if, for whatever reason, the player manages to start another dialogue prior to finishing another
        if (!isDialogueFinished)
        {
            foreach (var VARIABLE in _choices)
            {
                Destroy(VARIABLE);
            }
            _choices.Clear();
            _choicesSelectionIndicator.Clear();
            StopAllCoroutines();
        }
        textBox.gameObject.SetActive(true);
        _displayingChoices = false;
        EventSystem.current.SetSelectedGameObject(null);
        isDialogueFinished = false;
        spriteContainer.SetActive(true);
        dialogueContainer.SetActive(true);
        speakerImage.gameObject.SetActive(true);
        
        choiceBox.SetActive(false);
        if (_choices.Count > 0)
            _choices.Clear();
        if(_textChoices.Count>0)
            _textChoices.Clear();
        if (PlayerPrefs.HasKey(PrefsKeySpeed))
        {
            _displaySpeed = PlayerPrefs.GetInt(PrefsKeySpeed);
        }
        else
        {
            _displaySpeed = 1;
            PlayerPrefs.SetInt(PrefsKeySpeed,1);
        }

        if (PlayerPrefs.HasKey(PrefsKeyAuto))
        {
            _autoScrollOnClick = PlayerPrefs.GetInt(PrefsKeyAuto) == 1;
        }
        else
        {
            _autoScrollOnClick = false;
            PlayerPrefs.SetInt(PrefsKeyAuto,0);
        }
        
        textBox.text = "";
        
        //empties the historyText list
        /*foreach (var historyText in _historyTextList)
        {
            Destroy(historyText);
        }
        */
        foreach (Transform child in historyContent.transform)
        {
            Destroy(child.gameObject);
        }
        _historyTextList.Clear();
        _previousSpeakerIndex = 0;
        
        _dialogueTree = DialogueNode.CreateDialogueNodes(textFile);
                
        _currentNode = 1;

        //creates the first name wrapper, adds the text to the wrapper, and updates the speaker variables
        GameObject nameWrapper = Instantiate(_nameWrapperPrefab, historyContent.transform, false);
        nameWrapper.GetComponent<VerticalLayoutGroup>().padding = new RectOffset();
        _historyTextList.Add(Instantiate(_nameCardHistoryPrefab,nameWrapper.transform,false).GetComponent<Text>());
        _historyTextList[0].text = nameTextBox.text = _dialogueTree[_currentNode].Name;

        
        _currentTextWrapper = Instantiate(_textWrapperPrefab, historyContent.transform, false).transform;

        _dialogueOn = true;
        _displayLine = DisplayLine();
        speakerImage.sprite = _speakerImages[_historyTextList[0].text];
       
        currentHistoryIndex = 0;

        StartCoroutine(DisplayLine());
    }

    public void EndDialogue()
    {
        isDialogueFinished = true;
        _dialogueOn = false;
        _displayingDialogue = false;
        dialogueEndEvent?.Invoke();
        /*if (delegatesStart.Count > 0)
        {
            foreach (var delegateStart in delegatesStart)
            {
                _dialogueStart -= delegateStart;
            }
        }

        if (delegatesEnd.Count > 0)
        {
            foreach (var delegateEnd in delegatesEnd)
            {
                _dialogueEnd -= delegateEnd;
            }
        }*/
        
        spriteContainer.SetActive(false);
        speakerImage.gameObject.SetActive(false);
        dialogueContainer.SetActive(false);
    }

    //this is literally only if we have a pause inside a dialogue file
    //this should be called externally (there is no way for the dialogue to unpause itself, nor should there be)
    public void UnPauseDialogue()
    {        
        paused = false;
        dialogueUnpauseEvent?.Invoke();
        textBox.gameObject.SetActive(true);
        spriteContainer.SetActive(true);
        dialogueContainer.SetActive(true);
        
        textBox.text = "";
        
        var nextMessage = _dialogueTree[_currentNode].Message;
        if (nextMessage.Equals("END"))
        {
            EndDialogue();
            return;
        }

        if (nextMessage.Equals("PAUSE"))
        {
            PauseDialogue();
            return;
        }
        NextLine(_currentNode);
    }

    public void PauseDialogue()
    {
        dialoguePauseEvent?.Invoke();
        paused = true;
        textBox.gameObject.SetActive(false);
       spriteContainer.SetActive(false);
/*
       _dialoguePaused?.Invoke();
*/
       dialogueContainer.SetActive(false);
    }
    
    //returns true if there is another line
    //returns false if there is no more lines to read
    private bool NextLine(int currentNode)
    {
        NextIndicator.SetActive(false);
        if (!speakerImage.enabled)
        {
            speakerImage.enabled = true;
        }
        StopCoroutine(_displayLine);
        /*if (currentNode == -1)
        {
            StopAllCoroutines();
            return false;
        }*/
        if (_dialogueTree[currentNode].Message.ToUpper().StartsWith("END"))
        {
            return false;
        }

        if (_dialogueTree[currentNode].Message.StartsWith("PAUSE"))
        {
            _currentNode = currentNode+1;
            PauseDialogue();
            return true;
        }
        
        var currentName = _dialogueTree[currentNode].Name;
        if (currentName.ToLower().StartsWith("null"))
        {
            speakerImage.sprite = speakerImageDefault;
            nameTextBox.text = "";
            
            GameObject wrapper = Instantiate(_nameWrapperPrefab,historyContent.transform,false);
            _historyTextList.Add(Instantiate(_nameCardHistoryPrefab, wrapper.transform, false).GetComponent<Text>());

            _previousSpeakerIndex = _historyTextList.Count - 1;
            _historyTextList[++currentHistoryIndex].text = " ";
            _currentTextWrapper = Instantiate(_textWrapperPrefab, historyContent.transform, false).transform;
        }
        else if (!nameTextBox.text.StartsWith(currentName))
        {
            nameTextBox.text = currentName;
            if (_speakerImages.ContainsKey(currentName))
            {
                speakerImage.sprite = _speakerImages[currentName];
            }
            else
            {
                //todo: probably set this to an unknown image sprite (like a sprite with a question mark)?
                speakerImage.sprite = speakerImageDefault;
                //may want to use a null sprite instead?
            }
            
            GameObject wrapper = Instantiate(_nameWrapperPrefab,historyContent.transform,false);
            _historyTextList.Add(Instantiate(_nameCardHistoryPrefab, wrapper.transform, false).GetComponent<Text>());

            _previousSpeakerIndex = _historyTextList.Count - 1;
            _historyTextList[++currentHistoryIndex].text = currentName;
            _currentTextWrapper = Instantiate(_textWrapperPrefab, historyContent.transform, false).transform;
            
        }
        _currentNode = currentNode;
       
        _displayLine = DisplayLine();
        textBox.text = "";
        StartCoroutine(_displayLine);
        return true;
    }

   
    //returns true if the player presses space, enter, or lmb
    private bool MoveDialogueForward()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
               Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    private IEnumerator DisplayLine()
    {
        if (_createdChoice)
        {
            _createdChoice = false;
            
            //it'll look better like this, trust me
            GameObject nameWrapper = Instantiate(_nameWrapperPrefab, historyContent.transform, false);
            _historyTextList.Add(Instantiate(_nameCardHistoryPrefab,nameWrapper.transform,false).GetComponent<Text>());
            _historyTextList[++currentHistoryIndex].text = _historyTextList[_previousSpeakerIndex].text;
            
            //creates a new text wrapper
            _currentTextWrapper = Instantiate(_textWrapperPrefab, historyContent.transform, false).transform;
        }

        var message = _dialogueTree[_currentNode].Message;
        
        _historyTextList.Add(Instantiate(_textHistoryPrefab,_currentTextWrapper,false).GetComponent<Text>());
        currentHistoryIndex++;
       
        //if the previous object in the list is a name card
        //then set an extra 5 units between the new object and old
        if (_previousSpeakerIndex + 1 == currentHistoryIndex)
        {
            _currentTextWrapper.GetComponent<VerticalLayoutGroup>().padding.top += 5;

        }

        _historyTextList[currentHistoryIndex].text = "";
       
/*
        _chars = message.ToCharArray();
*/
        _displayingDialogue = true;

        var sets = MaxVerticalWordDisplay(textBox,message);
        var startTime = 0f;
        char[] charsOfThisLine;

        _dialogueRemaining = true;
        for (var index = 0; index < sets.Count; index++)
        {
            _displayingDialogue = true;
            _displayAll = false;
            var set = sets[index];
            charsOfThisLine = set.ToCharArray();
            for (_lineIndex = 0; _lineIndex < charsOfThisLine.Length; _lineIndex++)
            {
                while (_historyDisplayed)
                {
                    yield return null;
                }

                if (_displayAll)
                {
                    for (; _lineIndex < charsOfThisLine.Length; _lineIndex++)
                    {
                        //stopcoroutine wasn't working, i'm guessing because it can't pause the actual loops?
                        while (_historyDisplayed)
                        {
                            yield return null;
                        }

                        textBox.text += charsOfThisLine[_lineIndex];
                    }

                    _displayAll = false;
                    //yield return null;
                    break;
                }

                textBox.text += charsOfThisLine[_lineIndex].ToString();


                //the delay between each character displayed
                if (!skipText)
                    yield return new WaitForSeconds(.05f / _displaySpeed);
                else
                {
                    yield return new WaitForSeconds(0.0005f);
                }
            }
            _displayingDialogue = false;
            
            //necessary so the textbox won't be blank at the end of line
            if (index + 1 == sets.Count)
            {
                break;
            }
            
            startTime = Time.time;
                
            //in order to not break the update method's check to skip all animations
            //waits 1.5secs before continuing the message
            //we do this instead of instantly displaying the rest of the message because, depending on the text speed
            //not everyone would have finished reading the initial half of the message
            NextIndicator.SetActive(true);               
            _historyTextList[currentHistoryIndex].text += textBox.text;
            

            while (!skipText && (!MoveDialogueForward()|| (_autoScrollOnClick &&Time.time - startTime>=1.5f)))
            {
                //copied and pasted from above
                //in case the player decides to open the history, we do not want auto scroll to activate
                while (_historyDisplayed&&!skipText)
                {
                   
                    //this is lazy, right? It can't be this easy...
                    startTime += Time.deltaTime;

                    yield return null;
                }
                yield return null;
            }
            NextIndicator.SetActive(false);
            //todo?maybe i'll make a crappy text scrolling animation idk
            textBox.text = "";
            
        }
        _dialogueRemaining = false;
        _historyTextList[currentHistoryIndex].text += textBox.text;

        var currentNode = _dialogueTree[_currentNode];
        
        //checks if the connections are choices and
        //if the choices are displayed
        //i'm doing this because for some reason holding control while selecting a choice would sometimes call this instead
        //it shouldn't, really shouldn't but I can't figure it out
        if (_dialogueTree[_currentNode].ConnectionsAreChoices)
        {
            if(!_displayingChoices)
                DisplayChoices(currentNode.Connection.Count);
        }
        //since we don't want the indicator to appear when there are choices to be made
        //indicator implies that the player can click to move on
        else
            NextIndicator.SetActive(true);
    }

    public void DisplayHistory()
    {
        _historyDisplayed = true;
        historyParent.SetActive(true);
        historyScrollRect.normalizedPosition =new Vector2(0,0);
    }

    public void CloseHistory()
    {
        _historyDisplayed = false;
        historyParent.SetActive(false);
    }
    
    //CHOICES METHODS
    private void DisplayChoices(int choiceCount)
    {
        choiceBox.SetActive(true);
        if (choiceCount < 1)
            return;
        _choices.Clear();
        _textChoices.Clear();

        DialogueNode node = _dialogueTree[_currentNode];
                                                         
        /*else*/
        //90 between all items
        //63 height
        if (_choices.Capacity < choiceCount)
            _choices.Capacity = choiceCount;
        if (_choicesSelectionIndicator.Capacity < choiceCount)
            _choicesSelectionIndicator.Capacity = choiceCount;
        
        //creates a new section containing the choices in the history
        GameObject choiceHistoryWrapper = Instantiate(_choiceWrapperPrefab, historyContent.transform, false);
        //its either the first or second object, and it better follow the stupid programming index conventions
        GameObject choiceHistoryContent = choiceHistoryWrapper.transform.GetChild(1).gameObject;
            
        //this code gives us a list of choices to be used for adding the buttons
        List<DialogueNode> choices = new List<DialogueNode>(choiceCount);
        List<int> nodeInts = new List<int>(choiceCount);
        
        //this code only adds the choices (from the connections list) that have not been viewed yet
        for (int i = 0;i<node.Connection.Count;i++)
        {
            //connection is an integer of the node value in the DialogueTree
            var connection = node.Connection[i];
            if (!_dialogueTree[connection].ChoiceHasBeenViewed)
            {
                choices.Add(_dialogueTree[connection]);
                nodeInts.Add(connection);
            }
        }
        
        for (int i = 0; i < choiceCount && i < choices.Count; i++)
        {
            DialogueNode choice = choices[i];
            GameObject tempGameObject = Instantiate(_choiceButton, choiceBox.transform);
            _choices.Add(tempGameObject);
               
            //the name of the buttons are their nodes
            _choices[i].name = nodeInts[i].ToString();
            Text txt =
                _choices[i].GetComponentInChildren<Text>();
            _textChoices.Add(txt.gameObject);
            txt.text = choice.Message;
            txt.gameObject.name = i.ToString();
                
                
            EventTrigger trigger = _choices[i].AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerEnter};
            entry.callback.AddListener((eventData) => {UpdateSelectedChoice(txt.gameObject.name);});
            trigger.triggers.Add(entry);
                
            //it's not an actual highlight, i'm just too lazy to rename the arrow
            _choicesSelectionIndicator.Add(_choices[i].transform.Find("Highlight").GetComponent<Image>());
            if (i == 0)
            {
                _choicesSelectionIndicator[i].gameObject.SetActive(true);
            }
            
            _choices[i].GetComponent<Button>().onClick.AddListener(MakeChoice);
                
            _historyTextList.Add(Instantiate(_textHistoryPrefab, choiceHistoryContent.transform, false).GetComponent<Text>());
            _historyTextList[++currentHistoryIndex].text = txt.text;
            _historyTextList[currentHistoryIndex].color = Color.gray;
        }
        _displayingChoices = true;
        _createdChoice = true;
        _currentSelectedChoice = 0;
    }

    //i think nameOfChoice is the name of the choice??????/??
    public void UpdateSelectedChoice(string nameOfChoice)
    {
        int i;
        if (!int.TryParse(nameOfChoice, out i))
        {
            return;
        }
        
        if (_currentSelectedChoice < -5)
        {
            _currentSelectedChoice = i;
            _choicesSelectionIndicator[i].gameObject.SetActive(true);
            var position = _textChoices[i].transform.localPosition;
            position = new Vector2(position.x+50, position.y);
            _textChoices[i].transform.localPosition = position;
            position = _choicesSelectionIndicator[i].transform.localPosition;
            position = new Vector2(position.x+50, position.y);
            _choicesSelectionIndicator[i].transform.localPosition = position;
            EventSystem.current.SetSelectedGameObject(_choices[_currentSelectedChoice]);
            return;
        }
        
        
        //removing effects from old choice
        var oldChoice = _textChoices[_currentSelectedChoice];
        var selectionPosition = _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition;
        selectionPosition = new Vector2(selectionPosition.x - 50, selectionPosition.y);
        _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition = selectionPosition;
        _choicesSelectionIndicator[_currentSelectedChoice].gameObject.SetActive(false);
        
        var localPosition = oldChoice.transform.localPosition;
        localPosition = new Vector2(localPosition.x-50,localPosition.y);
        oldChoice.transform.localPosition = localPosition;

        _currentSelectedChoice = i;

        var currentChoice = _textChoices[_currentSelectedChoice];
        
        selectionPosition = _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition;
        selectionPosition = new Vector2(selectionPosition.x + 50, selectionPosition.y);
        _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition = selectionPosition;
        _choicesSelectionIndicator[_currentSelectedChoice].gameObject.SetActive(true);
        localPosition = currentChoice.transform.localPosition;
        localPosition = new Vector2(localPosition.x+50, localPosition.y);
        currentChoice.transform.localPosition = localPosition;
        EventSystem.current.SetSelectedGameObject(_choices[_currentSelectedChoice]);
        
        /*
         *    var oldChoice = _choices[currentSelectedChoice];
                    //ChangeColor(_choicesHighlights[currentSelectedChoice],-1,-1,-1,0);
                    _choicesHighlights[currentSelectedChoice].gameObject.SetActive(false);
                    var localPosition = oldChoice.transform.localPosition;
                    localPosition = new Vector2(localPosition.x-50,localPosition.y);
                    oldChoice.transform.localPosition = localPosition;
                    if (--currentSelectedChoice < 0)
                    {
                        currentSelectedChoice = _choices.Count - 1;
                    }
                    var currentChoice = _choices[currentSelectedChoice];
                    //ChangeColor(_choicesHighlights[currentSelectedChoice],-1,-1,-1,.4f);
                    _choicesHighlights[currentSelectedChoice].gameObject.SetActive(true);
                    localPosition = currentChoice.transform.localPosition;
                    localPosition = new Vector2(localPosition.x+50, localPosition.y);
                    currentChoice.transform.localPosition = localPosition;
                    EventSystem.current.SetSelectedGameObject(_choices[currentSelectedChoice]);
         */
    }
    
    private void UpdateSelectedChoice(int newChoiceIndex)
    {
        if (newChoiceIndex < -5)
        {
            _currentSelectedChoice = 0;
            _choicesSelectionIndicator[0].gameObject.SetActive(true);
            var positionO = _textChoices[0].transform.localPosition;
            positionO = new Vector2(positionO.x+50, positionO.y);
            _textChoices[0].transform.localPosition = positionO;
            positionO = _choicesSelectionIndicator[0].transform.localPosition;
            positionO = new Vector2(positionO.x+50, positionO.y);
            _choicesSelectionIndicator[0].transform.localPosition = positionO;
            EventSystem.current.SetSelectedGameObject(_choices[_currentSelectedChoice]);
            return;
        }
        
        var selectionPosition = _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition;
        selectionPosition = new Vector2(selectionPosition.x - 50, selectionPosition.y);
        _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition = selectionPosition;
        _choicesSelectionIndicator[_currentSelectedChoice].gameObject.SetActive(false);
        
        var oldChoice = _textChoices[_currentSelectedChoice];
        var localPosition = oldChoice.transform.localPosition;
        localPosition = new Vector2(localPosition.x-50,localPosition.y);
        oldChoice.transform.localPosition = localPosition;

        //if the newChoiceIndex is less than 0, set currentSelectedChoice to the max index
        //if it's not, check if it is past the max index
        //if it is, set currentSelectedChoice to 0
        //if it isn't, set currentSelectedChoice to newChoiceIndex
        _currentSelectedChoice = newChoiceIndex < 0 ? _choices.Count - 1 : newChoiceIndex >= _choices.Count ? 0: newChoiceIndex;
        
        //wtf is this? it's been months since I made this code and I have no idea what this is...
        /*if (newChoiceIndex < _choices.Count&& newChoiceIndex >=0)
        {
            
        }*/
        var currentChoice = _textChoices[_currentSelectedChoice];
        
        selectionPosition = _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition;
        selectionPosition = new Vector2(selectionPosition.x + 50, selectionPosition.y);
        _choicesSelectionIndicator[_currentSelectedChoice].transform.localPosition = selectionPosition;
        _choicesSelectionIndicator[_currentSelectedChoice].gameObject.SetActive(true);
        localPosition = currentChoice.transform.localPosition;
        localPosition = new Vector2(localPosition.x+50, localPosition.y);
        currentChoice.transform.localPosition = localPosition;
        EventSystem.current.SetSelectedGameObject(_choices[_currentSelectedChoice]);
    }
    
    /// <summary>
    /// Uses the currently selected GameObject via the EventSystem to try to make a choice.
    /// Will fail if the name of the GameObject is not a valid int.
    /// <seealso cref="StoredMakeChoice"/>
    /// </summary>
    public void MakeChoice()
    {
        //tries to make the name of the object into an int, and checks if the value is a valid line number
        if (int.TryParse(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name,
            out var activatorName) && activatorName >0)
        {
            _dialogueTree[activatorName].ChoiceHasBeenViewed = true;
            _historyTextList[currentHistoryIndex - (_choices.Count-_currentSelectedChoice-1)].color = Color.red;
            choiceBox.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
            
            foreach (var choice in _choices)
            {
                Destroy(choice);   
            }
            _choices.Clear();
            _choicesSelectionIndicator.Clear();
            _displayingChoices = false;
            if (_dialogueTree[_dialogueTree[activatorName].Connection[0]].isEnd())
            {
                EndDialogue();
                StopAllCoroutines();
            }
            else if(_dialogueTree[activatorName].ConnectionsAreChoices)
            {
                _currentNode = activatorName;
                DisplayChoices(_dialogueTree[activatorName].Connection.Count);
            }
            else
            {
                NextLine(_dialogueTree[activatorName].Connection[0]);
            }

            return;
        }

    }

    /// <summary>
    /// Uses the stored integer in <see cref="_currentSelectedChoice"/> to make the choice as opposed to using the selected GameObject.
    /// Will not make a choice if the integer stored is an invalid index.
    /// <seealso cref="MakeChoice"/>
    /// </summary>
    private void StoredMakeChoice()
    {
        //if it's a negative value (shouldn't be able to be at this point with my code) or if it's greater than the amount of indexes in the array
        if (_currentSelectedChoice < 0 || _currentSelectedChoice >= _choices.Count || !int.TryParse(_choices[_currentSelectedChoice].name, out var choiceNodeNum))
            return;

        _dialogueTree[choiceNodeNum].ChoiceHasBeenViewed = true;
        
        //this is copied and pasted from above, expect it to break because of my inability to check my work
        _historyTextList[currentHistoryIndex - (_choices.Count-_currentSelectedChoice-1)].color = Color.red;
        choiceBox.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        foreach (var choice in _choices)
        {
            Destroy(choice);   
        }
        _choices.Clear();
        _choicesSelectionIndicator.Clear();
        _displayingChoices = false;
        if (_dialogueTree[_dialogueTree[choiceNodeNum].Connection[0]].isEnd())
        {
            EndDialogue();
            StopAllCoroutines();
        }
        else if(_dialogueTree[choiceNodeNum].ConnectionsAreChoices)
        {
            _currentNode = choiceNodeNum;
            DisplayChoices(_dialogueTree[choiceNodeNum].Connection.Count);
        }
        else
        {
            NextLine(_dialogueTree[choiceNodeNum].Connection[0]);
        }
    }
    
    //
    /*
    private IEnumerator AddChoicesToHistory()
    {
        yield return new WaitForEndOfFrame();

        currentHistoryIndex++;
        _historyTextList.Add(Instantiate(_textHistoryPrefab).GetComponent<Text>());
        _historyTextList[currentHistoryIndex].transform.SetParent(historyContent.transform);
        _historyTextList[currentHistoryIndex].transform.localPosition =
            new Vector2(40,
                _historyTextList[currentHistoryIndex-1].transform.localPosition.y-_historyTextList[currentHistoryIndex-1].preferredHeight+10);
        _historyTextList[currentHistoryIndex].text = "Choice";
        _historyTextList[currentHistoryIndex].color = Color.gray;

        
        foreach (var t in _choices)
        {
            var txt = t.GetComponentInChildren<Text>();
            currentHistoryIndex++;
            _historyTextList.Add(Instantiate(_textHistoryPrefab).GetComponent<Text>());
            _historyTextList[currentHistoryIndex].transform.SetParent(historyContent.transform);
            _historyTextList[currentHistoryIndex].transform.localPosition =
                new Vector2(80,
                    _historyTextList[currentHistoryIndex-1].transform.localPosition.y-_historyTextList[currentHistoryIndex-1].preferredHeight+10);
            _historyTextList[currentHistoryIndex].text = txt.text;
            _historyTextList[currentHistoryIndex].color = Color.gray;
        }
    }
    */
    
    
    
    //MISCELLANEOUS METHODS
    
    [Obsolete("This method does not work without updating the frame first. Use MaxVerticalWordDisplay instead.")]
    private bool TextLargerThanBox()
    {
        return 
            LayoutUtility.GetPreferredHeight(textBox.rectTransform) //This is the width the text would LIKE to be
            > _textBoxRectTransform.rect.height; //This is the actual width of the text's parent container
    }

    //this method name is way too long
    //would you rather have it be private bool x2(string v)?
    [Obsolete("This method is obsolete. Use MaxVerticalWordDisplay instead.")]
    private bool TextWillBeTallerThanBox(string text)
    {
        // textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(textComponent.text,settings)
        
        return textBox.cachedTextGeneratorForLayout.GetPreferredHeight(text, _generationSettings) > _textBoxRectTransform.rect.height;
    }

    /// <summary>
    /// Returns a list of strings delimited by the maximum amount of words that can fit inside <see cref="testText"/>.
    /// This stops vertical overflow of text. This does not stop horizontal overflow.
    /// </summary>
    /// <param name="testText">The text box to be tested.</param>
    /// <param name="message">The message to be inputted into <see cref="testText"/></param>
    /// <returns></returns>
    private List<string> MaxVerticalWordDisplay(Text testText, string message)
    {           
        var previousString = "";
        var currentString = "";
        //_generationSettings = textBox.GetGenerationSettings(textBox.rectTransform.rect.size);

        var rect = testText.rectTransform.rect;
        var generationSettings = testText.GetGenerationSettings(rect.size);
        var rectHeight =rect.height* PersistentCanvas.Instance.Canvas.scaleFactor;

        var textGenerator = testText.cachedTextGeneratorForLayout;
        //char[] chars = message.ToCharArray();

        //if the given message does not contain any spaces
        if (!message.Contains(" "))
        {
            //then we will run through each character instead and return the string which has as many characters within it as possible
                //Without overflowing the textbox height
                char[] chars = message.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                currentString += chars[i].ToString();
                //I'm going to test this by setting up a var that is the cachedTextGenerator
                //I'm not sure if it will work (since maybe pointers or whatever, reference numbers and whatnot) so just uncomment this if it doesn't
                //if(testText.cachedTextGeneratorForLayout.GetPreferredHeight(currentString,generationSettings)>)

                if (textGenerator.GetPreferredHeight(currentString, generationSettings) > rectHeight)
                {
                    return new List<string>{previousString};
                }

                previousString =currentString;
            }

            return new List<string>{currentString};
        }

        
        //I don't think we'll need more than 3 displays?
        List<string> strings = new List<string>(3);
        
        
        //this won't work, we want to separate the strings with the space at the start of each word
            //that's because we don't want a word getting cut out of the textbox because of blank spaces,
            //which doesn't accurately reflect what is visible
        /*string[] words = message.Split(' ');*/
        
        
        //we are using our own Split method (lol)
        //this splits it so that the delimiter is NOT included (wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww)
        //this may be a bit oversized, but reallocating it multiple times is worse for performance
        List<string> words = /* new List<string>(32);
        string word = "";
        for (int i = 0; i < chars.Length; i++)
        {
            
                //allocating too much space wastes resources, but allowing it to reallocate each time it adds something is more resource expensive
            var currentChar = chars[i];
            //wait can we just use a '==' ?
            //if the current character is a space
            if (currentChar.Equals(' '))
            {
                //then add the previous word into the array and reset the word variable
                words.Add(word);
                word = " ";
            }
            else
            {
                //otherwise, add the character to the current word
                word += currentChar.ToString();
            }
        }*/
            message.Split(' ').ToList();    

        //i assure you Mr. Powell, adding useless comments and random letters is necessary for me to code properly
        //this is done because we assume the last word does not have a whitespace afterwords.
        /*words.Add(word);*/
        
        //next we add the words to the strings

        for (int i = 0; i < words.Count; i++)
        {
            currentString += $" {words[i]}";
            
            //todo: this if statement fails
            var prefHeight = textGenerator.GetPreferredHeight(currentString, generationSettings);
            if ( prefHeight> rectHeight)
            {
                //removes a random space that would sometimes appear at the start of a message
                if (previousString.StartsWith(" "))
                    previousString = previousString.Substring(1, previousString.Length - 1);
                strings.Add(previousString);
                previousString = "";
                currentString = previousString = words[i];
            }
            //this is kinda useless since currentString would be set blank in the if statement...
            else
            {
                //adding the string is faster by like .3 ticks, but doing so requires accessing either the array first or creating a new variable, both of which make the code slower than setting it equal
                previousString = currentString;
            }
        }
        
        if (previousString.StartsWith(" "))
            previousString = previousString.Substring(1, previousString.Length - 1);
        strings.Add(previousString);
        return strings;
    }

    //mm yes i made a useless code
    /// <summary>
    /// Changes an <see cref="Image"/>'s <see cref="Color"/> to parameter values.
    /// </summary>
    /// <param name="i">The Image which color is to be changed.</param>
    /// <param name="r">The red value of the new color. Set to -1 to keep the previous value. Scale of 0 to 1.</param>
    /// <param name="g">The green value of the new color. Set to -1 to keep the previous value. Scale of 0 to 1.</param>
    /// <param name="b">The blue value of the new color. Set to -1 to keep the previous value. Scale of 0 to 1.</param>
    /// <param name="a">The alpha value of the new color. Set to -1 to keep the previous value. Scale of 0 to 1.</param>
    private void ChangeColor(Image i, float r, float g, float b, float a)
    {
        if (i is null || r > 1 || g > 1 || b > 1 || a > 1)
            return;
        var color = i.color;
        var newR = r <0 ? color.r : r;
        var newG = g<0 ? color.g : g;
        var newB = b <0 ? color.b : b;
        var newA = a <0 ? color.a : a;
        i.color = new Color(newR,newG,newB,newA);
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
            if (started) return;
            dialogueStartEvent=new UnityEvent();
            dialogueEndEvent=new UnityEvent();
            dialoguePauseEvent=new UnityEvent();
            dialogueUnpauseEvent=new UnityEvent();
            started = true;
            

//            DontDestroyOnLoad(gameObject);
            paused = false;
            isDialogueFinished = true;
            _textBoxRectTransform = textBox.GetComponent<RectTransform>();

            //CHECKING FOR PLAYER PREFS
            if (PlayerPrefs.HasKey(PrefsKeySpeed))
            {
                _displaySpeed = PlayerPrefs.GetInt(PrefsKeySpeed);
            }
            else
            {
                _displaySpeed = 1;
                PlayerPrefs.SetInt(PrefsKeySpeed, 1);
            }

            if (PlayerPrefs.HasKey(PrefsKeyAuto))
            {
                _autoScrollOnClick = PlayerPrefs.GetInt(PrefsKeyAuto) == 1;
            }
            else
            {
                _autoScrollOnClick = false;
                PlayerPrefs.SetInt(PrefsKeyAuto, 0);
            }

            if (PlayerPrefs.HasKey(PrefsKeySkipTextToggle))
            {
                skipTextModeToggle = PlayerPrefs.GetInt(PrefsKeySkipTextToggle) == 1;
            }
            else
            {
                skipTextModeToggle = false;
                PlayerPrefs.SetInt(PrefsKeySkipTextToggle, 0);
            }
            //END OF CHECKING FOR PLAYER PREFS

            //for now, the default capacity is 3 since i don't expect us to run that many choices
            _choices = new List<GameObject>(3);
            _currentSelectedChoice = 0;
            _choicesSelectionIndicator = new List<Image>(3);
            _displayingChoices = false;
            _textChoices = new List<GameObject>(3);


            var img = Resources.LoadAll<Sprite>("Character Portraits");
            _textChoices = new List<GameObject>(3);

            _speakerImages = new Dictionary<string, Sprite>(img.Length);
            foreach (var sprite in img)
            {
                _speakerImages.Add(sprite.name, sprite);
            }
            
            _textGenerator=new TextGenerator();
           
            _generationSettings = textBox.GetGenerationSettings(textBox.rectTransform.rect.size);

            historyParent.SetActive(false);
            _historyTextList = new List<Text>();
            _choiceButton = Resources.Load("Prefabs/choiceButtonPrefab") as GameObject;
            _nameCardHistoryPrefab = Resources.Load("Prefabs/Name Card (History)") as GameObject;
            _textHistoryPrefab = Resources.Load("Prefabs/Text (History)") as GameObject;
            _choiceWrapperPrefab = Resources.Load("Prefabs/Choices Wrapper") as GameObject;
            _nameWrapperPrefab = Resources.Load("Prefabs/Name Card Wrapper") as GameObject;
            _textWrapperPrefab = Resources.Load("Prefabs/TextBox Wrapper") as GameObject;
            speakerImageDefault = speakerImage.sprite;
            _createdChoice = false;
        }
    }
}
