using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class SimonSmothersScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;

    public Light bigLight;
    public TextMesh goofyAhhText;
    private static Coroutine _bigFlashCoroutine;
    private static bool anyActive;
    private bool thisActive;
    private bool cbOn;

    public KMSelectable[] buttons; //buttons[4] is the submit button
    public Light[] dirLights;
    private Coroutine[] buttonCoroutines = new Coroutine[5];

    private int stage;
    private bool pressedSubmitAlready;
    List<int> buttonSounds = Enumerable.Range(1,4).ToList();
    int submitSound;

    private Pattern generatedPattern, inputtedPattern;
    private List<Flash> flashes = new List<Flash>();

    private static readonly int[][] sideLengths = new int[][]
    {
        new[] { 2, 3, 2, 3 },
        new[] { 3, 2, 3, 2 },
        new[] { 3, 3, 2, 2 },
        new[] { 2, 2, 3, 3 },
        new[] { 3, 2, 2, 3 },
    };
    private static readonly Dictionary<RGBColor, Color> colorLookup = new Dictionary<RGBColor, Color>()
    {
        { RGBColor.Red, Color.red },
        { RGBColor.Green, Color.green },
        { RGBColor.Blue, Color.blue },
        { RGBColor.Cyan, Color.cyan },
        { RGBColor.Magenta, Color.magenta },
        { RGBColor.Yellow, Color.yellow },
        { RGBColor.Black, Color.black },
        { RGBColor.White, Color.white },
    };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < 5; i++)
        {
            int ix = i;
            buttons[ix].OnInteract += delegate () { ButtonPress(ix); return false; };
        }
    }

    void Start ()
    {
        for (int i = 0; i < 5; i++)
            dirLights[i].range *= transform.lossyScale.x;
        bigLight.range *= transform.lossyScale.x;
        buttonSounds.Shuffle();
        buttonSounds.Add(Rnd.Range(1, 5));
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
    }

    void ButtonPress(int position)
    {
        buttons[position].AddInteractionPunch(0.25f);
        if (buttonCoroutines[position] != null)
            StopCoroutine(buttonCoroutines[position]);
        buttonCoroutines[position] = StartCoroutine(FlashLight(dirLights[position], 0.33f, true));
        Audio.PlaySoundAtTransform("Sound" + buttonSounds[position], buttons[position].transform);
        if (moduleSolved)
            return;

        if (position == 4)
            HandleSubmit();
        else
            HandleDirectionPress((Dir)position);
        
    }

    void HandleSubmit()
    {
        if (!thisActive)
        {
            if (anyActive)
            {
                Log("Attempted to start the module while one was already active. Strike!");
                Module.HandleStrike();
            }
            else
            {
                thisActive = true;
                anyActive = true;
                GenerateStage();
                _bigFlashCoroutine = StartCoroutine(FlashThisStage());
            }
        }
        else if (pressedSubmitAlready)
        {
            pressedSubmitAlready = false;
            if (generatedPattern.IsEquivalentPattern(inputtedPattern))
            {
                if (stage == 4)
                    StartCoroutine(Solve());
                else
                {
                    Audio.PlaySoundAtTransform("Pass", transform);
                    _bigFlashCoroutine = StartCoroutine(FlashThisStage());
                    GenerateStage();
                    inputtedPattern = Pattern.center;
                }

            }
            else
            {
                Log("Inputted the following pattern, which is incorrect:");
                LogPattern(inputtedPattern, false);
                Strike();
            }
        }
        else
        {
            pressedSubmitAlready = true;
            if (_bigFlashCoroutine != null)
                StopCoroutine(_bigFlashCoroutine);
        }
    }
    IEnumerator Solve()
    {
        Audio.PlaySoundAtTransform("Solve", transform);
        moduleSolved = true;
        anyActive = false;
        yield return new WaitForSecondsRealtime(3.456f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        Module.HandlePass();
    }
    void Strike()
    {
        Module.HandleStrike();
        pressedSubmitAlready = false;
        _bigFlashCoroutine = StartCoroutine(FlashThisStage());
        inputtedPattern = Pattern.center;
    }
    void HandleDirectionPress(Dir pressedDir)
    {
        if (!thisActive)
            return;
        StopBigFlash();

        if (!inputtedPattern.AddInDir(pressedDir, pressedSubmitAlready))
        {
            Log("Attempted to move onto a cell already painted. Strike!");
            Strike();
        }
        pressedSubmitAlready = false;
    }
    void GenerateStage()
    {
        Flash generatedFlash;
        Pattern p;
        do
        {
            generatedFlash = new Flash((Dir)Rnd.Range(0, 4), 
                                (RGBColor)Rnd.Range(1, 7), 
                                sideLengths[Bomb.GetSerialNumberNumbers().First() % 5][stage]);
            p = new Pattern();
            List<Flash> newFlashes = flashes.ToList();
            newFlashes.Add(generatedFlash);
            for (int flashIx = 0; flashIx < newFlashes.Count; flashIx++)
                p.Add(
                    newFlashes[flashIx].associatedSquare
                                    .Select(coord => coord.ApplyMovements( RotateDirections(newFlashes
                                                                                                .Select(fl => fl.direction)
                                                                                                .Take(flashIx + 1)
                                                                                            , flashIx))));
        } while (!p.IsPaintable());
        generatedPattern = new Pattern(p);
        flashes.Add(generatedFlash);
        LogStage();
        stage++;
        inputtedPattern = Pattern.center;
    }

    void LogStage()
    {
        Log("Added flash {0}.", flashes.Last());
        Log("Total flashes: {0}.", flashes.Join(", "));
        Log("Added a {0}×{0} square in color {1} in the center of the grid, and then moved in the directions: {2}.",
             sideLengths[Bomb.GetSerialNumberNumbers().Last() % 5][flashes.Count - 1],
             flashes.Last().color,
             RotateDirections(flashes.Select(x => x.direction), flashes.Count - 1).Join(", "));
        Log("The generated pattern is as follows:");
        LogPattern(generatedPattern, true);
    }

    void StopBigFlash()
    {
        bigLight.enabled = false;
        if (_bigFlashCoroutine != null)
            StopCoroutine(_bigFlashCoroutine);
    }

    IEnumerable<Dir> RotateDirections(IEnumerable<Dir> initial, int rotationNum)
    {
        return initial.Select(d => (Dir)(((int)d + rotationNum) % 4));
    }

    IEnumerator FlashLight(Light light, float time, bool isButton, RGBColor? used = null)
    {
        light.enabled = true;
        light.gameObject.SetActive(true);
        light.intensity = (used == RGBColor.Cyan || used == RGBColor.Magenta || used == RGBColor.Yellow) ? 0.3f : 0.5f;
        yield return new WaitForSeconds(time);
        if (isButton)
            light.gameObject.SetActive(false);
        light.enabled = false;
    }
    IEnumerator FlashThisStage()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);
            for (int stage = 0; stage < flashes.Count; stage++)
            {

                bigLight.color = colorLookup[flashes[stage].color];
                StartCoroutine(FlashLight(bigLight, 0.75f, false, flashes[stage].color));
                goofyAhhText.text = flashes[stage].color.ToString().ToUpper() + "!";
                StartCoroutine(FlashLight(dirLights[(int)flashes[stage].direction], 0.75f, true));
                if (cbOn)
                    StartCoroutine(FlashCBText(0.75f));
                Audio.PlaySoundAtTransform("Sound" + ((int)flashes[stage].direction + 1), transform);
                yield return new WaitForSeconds(1f);
            }

        }
    }
    IEnumerator FlashCBText(float d)
    {
        goofyAhhText.gameObject.SetActive(true);
        yield return new WaitForSeconds(d);
        goofyAhhText.gameObject.SetActive(false);
    }

    void OnDestroy()
    { anyActive = false; }

    void Log(string msg, params object[] args)
    {
        Debug.LogFormat("[Simon Smothers #{0}] {1}", moduleId, string.Format(msg, args));
    }
    void LogPattern(Pattern pattern, bool usingColors)
    {
        foreach (string line in pattern.GetLoggingPattern(usingColors))
            Log(line);
        if (!usingColors)
        {
            Log("The submitted tile differences are as follows: (Coordinates are column, then row; top-left is (0,0))");
            Log(pattern.GetLoggingDifferences());
        }
    }
    void ToggleCB()
    {
        cbOn = !cbOn;
        goofyAhhText.gameObject.SetActive(cbOn);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} URDLS> to press the up, right, down, left, then submit button. Use <!{0} colorblind> to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator Press(KMSelectable btn, float delay)
    {
        btn.OnInteract();
        yield return new WaitForSeconds(delay);
    }
    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (command.EqualsAny("COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB"))
        {
            yield return null;
            ToggleCB();
        }
        Match m = Regex.Match(command, @"^(?:(?:PRESS|SUBMIT|MOVE|P|M)\s+)?([URDLS]+)$");
        if (m.Success)
        {
            yield return null;
            foreach (char ch in m.Groups[1].Value)
            {
                buttons["URDLS".IndexOf(ch)].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
            if (moduleSolved)
                yield return "solve";
        }
    }

}
