﻿using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// </summary>
public class ColourFlashModule : MonoBehaviour
{
    #region Nested Types
    private class ColourPair
    {
        public ColourPair(Colours colourText, Colours colourValue)
        {
            ColourText = colourText;
            ColourValue = colourValue;
        }

        public Colours ColourText;
        public Colours ColourValue;

        public string Text
        {
            get
            {
                return ColourText.ToString();
            }
        }

        public Color DisplayColour
        {
            get
            {
                return ColourAttribute.GetColourFromEnum(ColourValue);
            }
        }

        public bool HasColour(Colours colour)
        {
            return ColourText == colour || ColourValue == colour;
        }
    }

    private class ColourAttribute : Attribute
    {
        public ColourAttribute(float r, float g, float b)
        {
            Colour = new Color(r, g, b, 1.0f);
        }

        public static Color GetColourFromEnum<T>(T enumValue)
        {
            var type = enumValue.GetType();
            var memInfo = type.GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(ColourAttribute), false);
            return (attributes.Length > 0) ? ((ColourAttribute)attributes[0]).Colour : Color.white;
        }

        public readonly Color Colour;
    }
    #endregion

    #region Enumerations
    public enum Colours
    {
        [Colour(1.0f, 0.0f, 0.0f)]
        Red,

        [Colour(1.0f, 1.0f, 0.0f)]
        Yellow,

        [Colour(0.0f, 1.0f, 0.0f)]
        Green,

        [Colour(0.0f, 0.0f, 1.0f)]
        Blue,

        [Colour(1.0f, 0.0f, 1.0f)]
        Magenta,

        [Colour(1.0f, 1.0f, 1.0f)]
        White
    }
    #endregion

    #region Related Components
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public ColourFlashButton ButtonYes;
    public ColourFlashButton ButtonNo;
    public TextMesh Indicator;
    #endregion

    #region Extra Preset Data
    public float TimePerCycleTick;
    public float TimePerCycleEnd;
    public const int NumberOfColoursInSequence = 8;
    public const float ProbabilityOfSameColourAndValue = 30.0f;
    #endregion

    #region Per-Module Data
    private delegate bool RuleButtonPressHandler(bool yesPress);
    private RuleButtonPressHandler _ruleButtonPressHandler = null;
    private ColourPair[] _colourSequence = null;
    private int _currentColourSequenceIndex = -1;
    private bool _solved = false;

    private ColourPair CurrentColourPair
    {
        get
        {
            if (_currentColourSequenceIndex == -1 || _colourSequence == null)
            {
                return null;
            }

            return _colourSequence[_currentColourSequenceIndex];
        }
    }
    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        BombModule.GenerateLogFriendlyName();
    }

    void Start()
    {
        StringBuilder logString = new StringBuilder();

        logString.Append("Module generated with the following word-color sequence:\n");
        logString.Append("# | Word    | Color   | Valid Response\n");
        logString.Append("--+---------+---------+----------------\n");

        string blockTitle = "";
        string condition = "";

        GenerateModuleInformation();

        RuleButtonPressHandler ruleHandler = SetupRules(ref blockTitle, ref condition);
        SetRuleButtonPressHandler(ruleHandler);

        //Logging section
        for(int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length; ++colourSequenceIndex)
        {
            _currentColourSequenceIndex = colourSequenceIndex;
            ColourPair pair = _colourSequence[_currentColourSequenceIndex];
            string response = ruleHandler(true) ? "[YES]" : (ruleHandler(false) ? "[NO]" : "---");

            logString.AppendFormat("{0} | {1,-7} | {2,-7} | {3}\n", colourSequenceIndex + 1, pair.ColourText.ToString(), pair.ColourValue.ToString(), response);
        }
        logString.Append("\nThe sequence matched the following block title and condition statement:\n");
        logString.AppendFormat("\"{0}\"\n-> \"{1}\"\n", blockTitle, condition);

        BombModule.Log(logString.ToString());

        _currentColourSequenceIndex = -1;

        BombModule.OnActivate += HandleModuleActive;

        ButtonYes.KMSelectable.OnInteract += HandlePressYes;
        ButtonNo.KMSelectable.OnInteract += HandlePressNo;
    }

    void OnDestroy()
    {
        HandleModuleInactive();
    }
    #endregion

    #region Handlers
    void HandleModuleActive()
    {
        StartCoroutine("ColourCycleCoroutine");
    }

    void HandleModuleInactive()
    {
        StopCoroutine("ColourCycleCoroutine");
    }

    bool HandlePressYes()
    {
        CheckRuleButtonPressHandler(true);
        return false;
    }

    bool HandlePressNo()
    {
        CheckRuleButtonPressHandler(false);
        return false;
    }
    #endregion

    #region Module Generation
    void GenerateModuleInformation()
    {
        Array colourChoices = Enum.GetValues(typeof(Colours));

        _colourSequence = new ColourPair[NumberOfColoursInSequence];
        for (int colourIndex = 0; colourIndex < NumberOfColoursInSequence; ++colourIndex)
        {
            float sameRoll = UnityEngine.Random.Range(0.0f, 100.0f);
            int textIndex = UnityEngine.Random.Range(0, colourChoices.Length);
            int valueIndex = textIndex;

            if (sameRoll >= ProbabilityOfSameColourAndValue)
            {
                valueIndex = UnityEngine.Random.Range(0, colourChoices.Length);
            }

            Colours colourText = (Colours)colourChoices.GetValue(textIndex);
            Colours colourValue = (Colours)colourChoices.GetValue(valueIndex);

            _colourSequence[colourIndex] = new ColourPair(colourText, colourValue);
        }
    }
    #endregion

    #region Module Updates
    IEnumerator ColourCycleCoroutine()
    {
        while (true)
        {
            for (int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length; ++colourSequenceIndex)
            {
                _currentColourSequenceIndex = colourSequenceIndex;
                Indicator.text = _colourSequence[colourSequenceIndex].Text;
                Indicator.color = _colourSequence[colourSequenceIndex].DisplayColour;
                yield return new WaitForSeconds(TimePerCycleTick);
            }

            _currentColourSequenceIndex = -1;

            Indicator.text = "";
            yield return new WaitForSeconds(TimePerCycleEnd);
        }
    }
    #endregion

    #region Module Rules
    private RuleButtonPressHandler SetupRules(ref string blockTitle, ref string condition)
    {
        CheckForClashingColours();

        blockTitle = string.Format("The color of the last word in the sequence is {0}.", _colourSequence[_colourSequence.Length - 1].ColourValue.ToString());

        switch (_colourSequence[_colourSequence.Length - 1].ColourValue)
        {
            case Colours.Red:
                return SetupRulesForBlockA(ref blockTitle, ref condition);
            case Colours.Yellow:
                return SetupRulesForBlockB(ref blockTitle, ref condition);
            case Colours.Green:
                return SetupRulesForBlockC(ref blockTitle, ref condition);
            case Colours.Blue:
                return SetupRulesForBlockD(ref blockTitle, ref condition);
            case Colours.Magenta:
                return SetupRulesForBlockE(ref blockTitle, ref condition);
            case Colours.White:
                return SetupRulesForBlockF(ref blockTitle, ref condition);

            default:
                return null;
        }
    }

    private void CheckForClashingColours()
    {
        Array colourChoices = Enum.GetValues(typeof(Colours));

        for (int colourIndex = 1; colourIndex < _colourSequence.Length; ++colourIndex)
        {
            if (_colourSequence[colourIndex - 1].ColourText == _colourSequence[colourIndex].ColourText && _colourSequence[colourIndex - 1].ColourValue == _colourSequence[colourIndex].ColourValue)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    _colourSequence[colourIndex].ColourText = (Colours)colourChoices.GetValue(((int)_colourSequence[colourIndex].ColourText + 1) % colourChoices.Length);
                }
                else
                {
                    _colourSequence[colourIndex].ColourValue = (Colours)colourChoices.GetValue(((int)_colourSequence[colourIndex].ColourValue + 1) % colourChoices.Length);
                }
            }
        }
    }

    private RuleButtonPressHandler SetupRulesForBlockA(ref string blockTitle, ref string condition)
    {
        if (_colourSequence.Count((x) => x.ColourText == Colours.Green) >= 3)
        {
            condition = "1. If Green is used as the word at least three times in the sequence, press Yes on the third time Green is used as either the word or the color of the word in the sequence.";
            return delegate (bool yesPress)
            {
                if (!yesPress)
                {
                    return false;
                }

                int greenCount = 0;
                for (int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length; ++colourSequenceIndex)
                {
                    if (_colourSequence[colourSequenceIndex].HasColour(Colours.Green))
                    {
                        greenCount++;
                        if (greenCount == 3)
                        {
                            return _currentColourSequenceIndex == colourSequenceIndex;
                        }
                    }
                }

                return false;
            };
        }

        if (_colourSequence.Count((x) => x.ColourValue == Colours.Blue) == 1)
        {
            condition = "2. (Otherwise,) if Blue is used as the color of the word exactly once, press No when the word Magenta is shown.";

            if (!_colourSequence.Any((x) => x.ColourText == Colours.Magenta))
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length)].ColourText = Colours.Magenta;

                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                return !yesPress && CurrentColourPair != null && CurrentColourPair.ColourText == Colours.Magenta;
            };
        }

        condition = "3. (Otherwise,) press Yes the last time White is either the word or the color of the word in the sequence.";

        if (!_colourSequence.Any((x) => x.HasColour(Colours.White)))
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourText = Colours.White;
            }
            else
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.White;
            }

            //Modification has been made to match this rule's exit condition - check against all pre-existing rules
            return SetupRules(ref blockTitle, ref condition);
        }

        return delegate (bool yesPress)
        {
            return yesPress && _currentColourSequenceIndex == Array.FindLastIndex(_colourSequence, (x) => x.HasColour(Colours.White));
        };
    }

    private RuleButtonPressHandler SetupRulesForBlockB(ref string blockTitle, ref string condition)
    {
        if (_colourSequence.Any((x) => x.ColourText == Colours.Blue && x.ColourValue == Colours.Green))
        {
            condition = "1. If the word Blue is shown in Green color, press Yes on the first time Green is used as the color of the word.";

            return delegate (bool yesPress)
            {
                return yesPress && _currentColourSequenceIndex == Array.FindIndex(_colourSequence, (x) => x.ColourValue == Colours.Green);
            };
        }

        if (_colourSequence.Any((x) => x.ColourText == Colours.White && (x.ColourValue == Colours.White || x.ColourValue == Colours.Red)))
        {
            condition = "2. (Otherwise,) if the word White is shown in either White or Red color, press Yes on the second time in the sequence where the color of the word does not match the word itself.";

            bool modified = false;
            while (_colourSequence.Count((x) => x.ColourText != x.ColourValue) < 2)
            {
                modified = true;
                ColourPair colourPair = _colourSequence.First((x) => x.ColourText == x.ColourValue);
                colourPair.ColourText = (Colours)(((int)colourPair.ColourText + 1) % Enum.GetValues(typeof(Colours)).Length);
            }

            if (modified)
            {
                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                if (!yesPress)
                {
                    return false;
                }

                int matchCount = 0;
                for (int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length; ++colourSequenceIndex)
                {
                    if (_colourSequence[colourSequenceIndex].ColourText != _colourSequence[colourSequenceIndex].ColourValue)
                    {
                        matchCount++;
                        if (matchCount == 2)
                        {
                            return _currentColourSequenceIndex == colourSequenceIndex;
                        }
                    }
                }

                return false;
            };
        }

        condition = "3. (Otherwise,) count the number of times Magenta is used as either the word or the color of the word in the sequence (the word Magenta in Magenta color only counts as one), and press No on the color in the total's position (e.g. a total of 4 means the fourth color in sequence).";

        //Otherwise, count the number of times Magenta is used as either the word or the colour of the word in the sequence, and press No on the colour in the total's position (i.e. a total of 4 means the fourth colour in sequence)
        if (!_colourSequence.Any((x) => x.HasColour(Colours.Magenta)))
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourText = Colours.Magenta;
            }
            else
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.Magenta;
            }

            //Modification has been made to match this rule's exit condition - check against all pre-existing rules
            return SetupRules(ref blockTitle, ref condition);
        }

        return delegate (bool yesPress)
        {
            return !yesPress && _currentColourSequenceIndex == _colourSequence.Count((x) => x.HasColour(Colours.Magenta)) - 1;
        };
    }

    private RuleButtonPressHandler SetupRulesForBlockC(ref string blockTitle, ref string condition)
    {
        for (int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length - 1; ++colourSequenceIndex)
        {
            condition = "1. If a word occurs consecutively with different colors, press No on the fifth entry in the sequence.";

            if (_colourSequence[colourSequenceIndex + 1].ColourText == _colourSequence[colourSequenceIndex].ColourText)
            {
                return delegate (bool yesPress)
                {
                    return !yesPress && _currentColourSequenceIndex == 4;
                };
            }
        }

        if (_colourSequence.Count((x) => x.ColourText == Colours.Magenta) >= 3)
        {
            condition = "2. (Otherwise,) if Magenta is used as the word as least three times in the sequence, press No on the first time Yellow is used as either the word or the color of the word in the sequence.";

            if (!_colourSequence.Any((x) => x.HasColour(Colours.Yellow)))
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourText = Colours.Yellow;
                }
                else
                {
                    _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.Yellow;
                }

                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                return !yesPress && _currentColourSequenceIndex == Array.FindIndex(_colourSequence, (x) => x.HasColour(Colours.Yellow));
            };
        }

        condition = "3. (Otherwise,) press Yes on any color where the color of the word matches the word itself.";

        if (!_colourSequence.Any((x) => x.ColourText == x.ColourValue))
        {
            int colourSequenceIndex = UnityEngine.Random.Range(0, _colourSequence.Length - 1);
            _colourSequence[colourSequenceIndex].ColourText = _colourSequence[colourSequenceIndex].ColourValue;

            //Modification has been made to match this rule's exit condition - check against all pre-existing rules
            return SetupRules(ref blockTitle, ref condition);
        }

        return delegate (bool yesPress)
        {
            return yesPress && CurrentColourPair.ColourText == CurrentColourPair.ColourValue;
        };
    }

    private RuleButtonPressHandler SetupRulesForBlockD(ref string blockTitle, ref string condition)
    {
        if (_colourSequence.Count((x) => x.ColourText != x.ColourValue) >= 3)
        {
            condition = "1. If the color of the word does not match the word itself three times or more in the sequence, press Yes on the first time in the sequence where the color of the word does not match the word itself.";

            return delegate (bool yesPress)
            {
                return yesPress && _currentColourSequenceIndex == Array.FindIndex(_colourSequence, (x) => x.ColourText != x.ColourValue);
            };
        }

        if (_colourSequence.Any((x) => (x.ColourText == Colours.Red && x.ColourValue == Colours.Yellow) || (x.ColourText == Colours.Yellow && x.ColourValue == Colours.White)))
        {
            condition = "2. (Otherwise,) if the word Red is shown in Yellow color, or the word Yellow is shown in White color, press No when the word White is shown in Red color.";

            if (!_colourSequence.Any((x) => x.ColourText == Colours.White && x.ColourValue == Colours.Red))
            {
                int colourSequenceIndex = Array.FindIndex(_colourSequence, (x) => !((x.ColourText == Colours.Red && x.ColourValue == Colours.Yellow) || (x.ColourText == Colours.Yellow && x.ColourValue == Colours.White)));
                _colourSequence[colourSequenceIndex].ColourText = Colours.White;
                _colourSequence[colourSequenceIndex].ColourValue = Colours.Red;

                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                return !yesPress && CurrentColourPair.ColourText == Colours.White && CurrentColourPair.ColourValue == Colours.Red;
            };
        }

        condition = "3. (Otherwise,) press Yes the last time Green is either the word or the color of the word in the sequence.";

        if (!_colourSequence.Any((x) => x.HasColour(Colours.Green)))
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourText = Colours.Green;
            }
            else
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.Green;
            }

            //Modification has been made to match this rule's exit condition - check against all pre-existing rules
            return SetupRules(ref blockTitle, ref condition);
        }

        return delegate (bool yesPress)
        {
            return yesPress && _currentColourSequenceIndex == Array.FindLastIndex(_colourSequence, (x) => x.HasColour(Colours.Green));
        };
    }

    private RuleButtonPressHandler SetupRulesForBlockE(ref string blockTitle, ref string condition)
    {
        for (int colourSequenceIndex = 0; colourSequenceIndex < _colourSequence.Length - 1; ++colourSequenceIndex)
        {
            condition = "1. If a color occurs consecutively with different words, press Yes on the third entry in the sequence.";

            if (_colourSequence[colourSequenceIndex + 1].ColourValue == _colourSequence[colourSequenceIndex].ColourValue)
            {
                return delegate (bool yesPress)
                {
                    return yesPress && _currentColourSequenceIndex == 2;
                };
            }
        }

        if (_colourSequence.Count((x) => x.ColourText == Colours.Yellow) > _colourSequence.Count((x) => x.ColourValue == Colours.Blue))
        {
            condition = "2. (Otherwise,) if the number of times the word Yellow appears is greater than the number of times that the color of the word is Blue, press No the last time the word Yellow is in the sequence.";

            return delegate (bool yesPress)
            {
                return !yesPress && _currentColourSequenceIndex == Array.FindLastIndex(_colourSequence, (x) => x.ColourText == Colours.Yellow);
            };
        }

        condition = "3. (Otherwise,) press No on the first time in the sequence where the color of the word matches the word of the seventh entry in the sequence.";

        Colours colourToMatch = _colourSequence[6].ColourText;
        if (!_colourSequence.Any((x) => x.ColourValue == colourToMatch))
        {
            _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = colourToMatch;

            //Modification has been made to match this rule's exit condition - check against all pre-existing rules
            return SetupRules(ref blockTitle, ref condition);
        }

        return delegate (bool yesPress)
        {
            return !yesPress && _currentColourSequenceIndex == Array.FindIndex(_colourSequence, (x) => x.ColourValue == colourToMatch);
        };
    }

    private RuleButtonPressHandler SetupRulesForBlockF(ref string blockTitle, ref string condition)
    {
        Colours colourToMatch = _colourSequence[2].ColourValue;
        if (_colourSequence[3].ColourText == colourToMatch || _colourSequence[4].ColourText == colourToMatch)
        {
            condition = "1. If the color of the third word matches the word of the fourth word or fifth word, press No the first time that Blue is used as the word or the color of the word in the sequence.";

            if (!_colourSequence.Any((x) => x.HasColour(Colours.Blue)))
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourText = Colours.Blue;
                }
                else
                {
                    _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.Blue;
                }

                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                return !yesPress && _currentColourSequenceIndex == Array.FindIndex(_colourSequence, (x) => x.HasColour(Colours.Blue));
            };
        }

        if (_colourSequence.Any((x) => x.ColourText == Colours.Yellow && x.ColourValue == Colours.Red))
        {
            condition = "2. (Otherwise,) if the word Yellow is shown in Red color, press Yes on the last time Blue is used as the color of the word.";

            if (!_colourSequence.Any((x) => x.ColourValue == Colours.Blue))
            {
                _colourSequence[UnityEngine.Random.Range(0, _colourSequence.Length - 1)].ColourValue = Colours.Blue;

                //Modification has been made to match this rule's exit condition - check against all pre-existing rules
                return SetupRules(ref blockTitle, ref condition);
            }

            return delegate (bool yesPress)
            {
                return yesPress && _currentColourSequenceIndex == Array.FindLastIndex(_colourSequence, (x) => x.ColourValue == Colours.Blue);
            };
        }

        condition = "3. (Otherwise,) press No.";

        return delegate (bool yesPress)
        {
            return !yesPress;
        };
    }

    private void SetRuleButtonPressHandler(RuleButtonPressHandler handler)
    {
        _ruleButtonPressHandler = handler;
    }

    private void CheckRuleButtonPressHandler(bool yesButton)
    {
        if (_currentColourSequenceIndex < 0)
        {
            BombModule.LogFormat("[{0}] button was pressed at an unknown time!", yesButton ? "YES" : "NO");
        }
        else
        {
            ColourPair pair = _colourSequence[_currentColourSequenceIndex];
            BombModule.LogFormat("[{0}] button was pressed on entry #{1} ('{2}' word in '{3}' color)", yesButton ? "YES" : "NO", _currentColourSequenceIndex + 1, pair.ColourText.ToString(), pair.ColourValue.ToString());
        }

        if (_ruleButtonPressHandler != null && _ruleButtonPressHandler(yesButton))
        {
            BombModule.Log("Valid answer! Module defused!");
            BombModule.HandlePass();
            FinishModule();
        }
        else
        {
            BombModule.Log("Invalid answer! Module triggered a strike!");
            BombModule.HandleStrike();
        }
    }

    private void FinishModule()
    {
        HandleModuleInactive();
        _solved = true;
    }
    #endregion

    #region Twitch Plays
    public IEnumerator ProcessTwitchCommand(string command)
    {
        //Because the goal-posts have changed and I didn't know until people blamed my un-aware implementation for breaking things.
        if (_solved)
        {
            yield break;
        }

        Debug.LogFormat("At the start, the time is: {0}", Time.time);

        Match modulesMatch = Regex.Match(command, "^press (yes|no|y|n) ([1-8]|any)$", RegexOptions.IgnoreCase);
        if (!modulesMatch.Success)
        {
            yield break;
        }

        KMSelectable buttonSelectable = null;

        string buttonName = modulesMatch.Groups[1].Value;
        if (buttonName.Equals("yes", StringComparison.InvariantCultureIgnoreCase) || buttonName.Equals("y", StringComparison.InvariantCultureIgnoreCase))
        {
            buttonSelectable = ButtonYes.KMSelectable;
        }
        else if (buttonName.Equals("no", StringComparison.InvariantCultureIgnoreCase) || buttonName.Equals("n", StringComparison.InvariantCultureIgnoreCase))
        {
            buttonSelectable = ButtonNo.KMSelectable;
        }

        if (buttonSelectable == null)
        {
            yield break;
        }

        //This is to "do the right thing"™
        yield return null;

        string position = modulesMatch.Groups[2].Value;
        int positionIndex = int.MinValue;

        if (int.TryParse(position, out positionIndex))
        {
            Debug.LogFormat("Just before the loop, the time is: {0}", Time.time);

            positionIndex--;
            while (positionIndex != _currentColourSequenceIndex)
            {
                Debug.LogFormat("In the loop, The time is: {0}", Time.time);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.LogFormat("End of the loop: {0}", Time.time);
            yield return buttonSelectable;
            yield return new WaitForSeconds(0.1f);
            yield return buttonSelectable;
        }
        else if (position.Equals("any", StringComparison.InvariantCultureIgnoreCase))
        {
            yield return buttonSelectable;
            yield return new WaitForSeconds(0.1f);
            yield return buttonSelectable;
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("Autosolve requested by Twitch Plays.");
        while (_ruleButtonPressHandler == null) yield return true;
        while (!_ruleButtonPressHandler(true) && !_ruleButtonPressHandler(false)) yield return true;
        if (_ruleButtonPressHandler(true))
            ButtonYes.KMSelectable.OnInteract();
        else
            ButtonNo.KMSelectable.OnInteract();
    }
    #endregion
}
