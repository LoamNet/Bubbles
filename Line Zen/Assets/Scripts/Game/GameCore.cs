﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCore : MonoBehaviour
{
    // Setup and linsk
    [Header("Base links")]
    public Data data;
    public Events events;
    public GameInputManager inputManager;

    // Static values
    public static readonly float widthLeeway = 0.025f;
    public static readonly float bubbleRadius = .3f;
    public static readonly int bonusThreshold = 1;
    public static readonly int pointsPerBubble = 20;
    public static readonly int pointsPerBonusBubble = 10;
    public static readonly int maxBubblesOnScreen = 12;

    [Header("Levels")]
    public TextAsset tutorialOne;
    public TextAsset tutorialTwo;
    public TextAsset currentLevel;

    [Header("Internals")]
    public GameMode internalMode;
    public GameState internalState;

    [Header("Levels")]
    public List<TextAsset> levels;

    // Private internal state
    private bool wasDownPreviously;
    private DataPoint lastLineStart;
    private DataPoint lastLineEnd;

    private List<DataPoint> bubbles;
    private List<Tuple<DataPoint, DataPoint>> guideLines;
    private bool hasInit;

    // Unlimited info
    private Utils.WichmannRng rand;

    bool internalStateCurrentHasInit;
    public GameState State
    {
        get
        {
            return internalState;
        }
        set
        {
            internalState = value;
            internalStateCurrentHasInit = false;
            events.OnGameStateChange?.Invoke(internalState);
        } 
    }
    public GameMode Mode
    {
        get
        {
            return internalMode;
        }
        set
        {
            internalMode = value;
        }
    }

    private void Start()
    {
        hasInit = false;
        internalStateCurrentHasInit = false;
        bubbles = new List<DataPoint>();
        guideLines = new List<Tuple<DataPoint, DataPoint>>();
        wasDownPreviously = false;

        events.OnShowHelpToggle += (isOn) => {
            DataGeneral toModify = data.GetDataGeneral();
            toModify.displayHelp = isOn;
            data.SetDataGeneral(toModify);
        };

        events.OnShowParticlesToggle += (isOn) => {
            DataGeneral toModify = data.GetDataGeneral();
            toModify.displayParticles = isOn;
            data.SetDataGeneral(toModify);
        };

        events.OnGameStateChangeRequest += (state, mode) => { State = state; Mode = mode; };
    }

    private void Update()
    {
        if(!hasInit)
        {
            data.Initialize();
            events.OnGameStateChange?.Invoke(internalState);
            events.OnGameInitialized?.Invoke();
            hasInit = true;
        }

        switch (State)
        {
            case GameState.Startup:
                break;
            case GameState.Options:
                break;
            case GameState.TutorialOne:
                PopulateLevelBubbles(tutorialOne);
                UpdatePlayerLine();
                CheckIfDoneLevelBubbles(GameState.TutorialTwo);
                break;
            case GameState.TutorialTwo:
                PopulateLevelBubbles(tutorialTwo);
                UpdatePlayerLine();
                CheckIfDoneLevelBubbles(GameState.Game);
                ResetTutorialIfIncomplete();
                break;
            case GameState.Game:
                if (Mode == GameMode.ChallengeLevel)
                {
                    PopulateLevelBubbles(currentLevel);
                    UpdatePlayerLine();
                    CheckIfDoneLevelBubbles(GameState.Game);
                }
                else if (Mode == GameMode.Infinite)
                {
                    PopulateUnlimitedBubbles();
                    UpdatePlayerLine();
                    CheckIfDoneUnlimitedBubbles();
                }
                break;
            case GameState.Exit:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif 
                break;
        }
    }

    public void ResetTutorialIfIncomplete()
    {
        if(bubbles.Count == 1)
        {
            internalStateCurrentHasInit = false;
        }
    }

    public void PopulateLevelBubbles(TextAsset levelData)
    {
        if(!internalStateCurrentHasInit)
        {
            currentLevel = levelData;

            bubbles.Clear();
            guideLines.Clear();

            string content = levelData.text;
            string[] lines = content.Split('\n');

            foreach(string line in lines)
            {
                string[] split = line.Split('=');
                string key = split[0].Trim().ToLowerInvariant();
                string value = split[1].Trim();

                if(key.Equals("bubble"))
                {
                    string[] point = value.Split(',');
                    float x = float.Parse(point[0].Trim());
                    float y = float.Parse(point[1].Trim());

                    bubbles.Add(new DataPoint(x, y));
                }
                else if(key.Equals("line"))
                {
                    string[] multiplePoints = value.Split(':');
                    string[] point1 = multiplePoints[0].Split(',');
                    string[] point2 = multiplePoints[1].Split(',');

                    float x1 = float.Parse(point1[0].Trim());
                    float y1 = float.Parse(point1[1].Trim());
                    float x2 = float.Parse(point2[0].Trim());
                    float y2 = float.Parse(point2[1].Trim());

                    guideLines.Add(new Tuple<DataPoint, DataPoint>(
                            new DataPoint(x1, y1),
                            new DataPoint(x2, y2)));
                }
            }

            events.OnBubblesChange?.Invoke(bubbles);
            events.OnGuideLinesChange?.Invoke(guideLines);

            internalStateCurrentHasInit = true;
        }
    }

    private void CheckIfDoneLevelBubbles(GameState nextState)
    {
        if (currentLevel == null)
        {
            State = GameState.Game;
            return;
        }

        if (bubbles.Count < 1)
        {
            State = nextState;
            return;
        }
    }

    private void CheckIfDoneUnlimitedBubbles()
    {
        if (bubbles.Count < 1)
        {
            DataGeneral gen = data.GetDataGeneral();
            gen.level = gen.level + 1;
            data.SetDataGeneral(gen);

            State = GameState.Game;
            return;
        }
    }

    private void PopulateUnlimitedBubbles()
    {
        if (!internalStateCurrentHasInit)
        {
            int level = data.GetDataGeneral().level;

            rand = new Utils.WichmannRng(level);
            bubbles.Clear();
            guideLines.Clear();

            while (bubbles.Count < (level / 2) + 2 && bubbles.Count < maxBubblesOnScreen)
            {
                double x = (rand.Next() - 0.5f) * 2;
                double y = (rand.Next() - 0.5f) * 2;
                DataPoint screenSize = inputManager.ScreenSizeWorld();
                DataPoint pos = new DataPoint(x * (screenSize.X - bubbleRadius), y * (screenSize.Y - bubbleRadius * 2));
                bubbles.Add(pos);
            }

            events.OnBubblesChange?.Invoke(bubbles);
            events.OnGuideLinesChange?.Invoke(guideLines);

            internalStateCurrentHasInit = true;
        }
    }

    // Handles updating positions for the player line, along with line starting and finishing events.
    private void UpdatePlayerLine()
    {
        if (inputManager.PrimaryInputDown())
        {
            if (wasDownPreviously)
            {
                lastLineEnd = inputManager.PrimaryInputPosWorld();
                events.OnLineUpdated?.Invoke(lastLineStart, lastLineEnd);
            }
            else
            {
                lastLineStart = inputManager.PrimaryInputPosWorld();
                lastLineEnd = inputManager.PrimaryInputPosWorld();
                events.OnLineCreated?.Invoke(lastLineStart, lastLineEnd);
            }

            wasDownPreviously = true;
        }
        else
        {
            if (wasDownPreviously)
            {
                DataEarnedScore points = CollectBubblesAsNecessary();
                events.OnBubblesChange?.Invoke(bubbles);
                events.OnLineDestroyed?.Invoke(lastLineStart, lastLineEnd, points);
                wasDownPreviously = false;
            }
        }
    }

    // Returns how much the score changed by
    private DataEarnedScore CollectBubblesAsNecessary()
    {
        float triggerRadius = bubbleRadius + VisualLineManager.width / 2 + GameCore.widthLeeway;

        List<DataPoint> locs = new List<DataPoint>();
        List<int> collectedIndexes = new List<int>();

        // Collect collisions
        for (int i = bubbles.Count - 1; i >= 0; --i)
        {
            DataPoint bubble = bubbles[i];

            bool isHit = Utils.IsLineTouchingCircle(lastLineStart, lastLineEnd, bubble, triggerRadius, bubbleRadius);
            
            if(isHit)
            {
                collectedIndexes.Add(i);
                locs.Add(bubble);
            }
        }

        // Score updating
        int hit = collectedIndexes.Count;
        int scoreBase = GameCore.pointsPerBubble * hit;
        int scoreBonus = 0;
        
        if(hit > bonusThreshold)
        {
            int bonusHits = hit - bonusThreshold;
            scoreBonus = bonusHits * bonusHits * pointsPerBonusBubble;
        }

        DataEarnedScore dataEarnedScore = new DataEarnedScore(scoreBase, scoreBonus, locs);

        if (pointsPerBubble != 0)
        {
            DataGeneral gen = data.GetDataGeneral();
            gen.score = gen.score + dataEarnedScore.total;
            data.SetDataGeneral(gen);
        }

        // Clear colleted bubbles
        foreach (int index in collectedIndexes)
        {
            events.OnBubbleDestroyed?.Invoke(bubbles[index]);
            bubbles.RemoveAt(index);
        }

        return dataEarnedScore;
    }
}
