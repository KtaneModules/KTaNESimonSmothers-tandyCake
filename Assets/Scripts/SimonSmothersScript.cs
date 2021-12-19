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

    public Light bigLight;
    private static Light _usedLight;
    private static Coroutine _bigFlashCoroutine;
    private static Light[] _allLights;
    private static bool anyActive;
    private bool thisActive;

    public KMSelectable[] buttons; //buttons[4] is the submit button
    public Light[] dirLights;
    private Coroutine[] buttonCoroutines = new Coroutine[5];

    private int stage;
    private bool pressedSubmitAlready;
    private int currentPaintColor;

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
        StartCoroutine(DetermineLight());
        for (int i = 0; i < 5; i++)
            dirLights[i].range *= transform.lossyScale.x;
        
    }

    void ButtonPress(int position)
    {
        buttons[position].AddInteractionPunch(0.25f);
        if (thisActive || position == 4)
        {
            if (buttonCoroutines[position] != null)
                StopCoroutine(buttonCoroutines[position]);
            buttonCoroutines[position] = StartCoroutine(FlashLight(dirLights[position], 0.33f));
            //Audio.PlaySoundAtTransform("808bruhhhhhhhhhhhhhhhhhhhhhh", buttons[position].transform);
        }

        if (position == 4)
            HandleSubmit();
        else
        {
            HandleDirectionPress((Dir)position);
        }
        
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
            if (generatedPattern.Equals(inputtedPattern))
            {
                if (stage == 3)
                    Module.HandlePass();
                else
                {
                    _bigFlashCoroutine = StartCoroutine(FlashThisStage());
                    GenerateStage();
                }

            }
            else
            {
                Log("Inputted the following pattern, which is incorrect:");
                LogPattern(inputtedPattern, false);
                Module.HandleStrike();
            }
            inputtedPattern = Pattern.center;
        }
        else
        {
            currentPaintColor++;
            pressedSubmitAlready = true;

            if (_bigFlashCoroutine != null)
                StopCoroutine(_bigFlashCoroutine);
        }
    }
    void HandleDirectionPress(Dir pressedDir)
    {
        if (!thisActive)
            return;
        StopBigFlash();
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
                                sideLengths[Bomb.GetSerialNumberNumbers().Last() % 5][stage]);
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
        Log("Added a {0}x{0} square in color {1} in the center of the grid, and then moved in the directions: {2}.",
             sideLengths[Bomb.GetSerialNumberNumbers().Last() % 5][flashes.Count - 1],
             flashes.Last().color,
             RotateDirections(flashes.Select(x => x.direction), flashes.Count - 1).Join(", "));
        Log("The generated pattern is as follows:");
        LogPattern(generatedPattern, true);
    }

    void StopBigFlash()
    {
        _usedLight.enabled = false;
        if (_bigFlashCoroutine != null)
            StopCoroutine(_bigFlashCoroutine);
    }

    IEnumerable<Dir> RotateDirections(IEnumerable<Dir> initial, int rotationNum)
    {
        return initial.Select(d => (Dir)(((int)d + rotationNum) % 4));
    }

    IEnumerator FlashLight(Light light, float time)
    {
        light.enabled = true;
        yield return new WaitForSeconds(time);
        light.enabled = false;
    }
    IEnumerator FlashThisStage()
    {
        Debug.Log("STARTR");
        while (true)
        {
            yield return new WaitForSeconds(2);
            for (int stage = 0; stage < flashes.Count; stage++)
            {
                _usedLight.color = colorLookup[flashes[stage].color];
                StartCoroutine(FlashLight(_usedLight, 0.75f));
                StartCoroutine(FlashLight(dirLights[(int)flashes[stage].direction], 0.75f));
                yield return new WaitForSeconds(1f);
            }

        }
    }

    IEnumerator DetermineLight()
    {
        yield return null;
        _allLights = FindObjectsOfType<SimonSmothersScript>().Select(x => x.bigLight).ToArray();
        Light closestLight = _allLights.OrderBy(L => Vector3.Distance(L.transform.position, Vector3.zero)).First();
        if (bigLight == closestLight)
        {
            Debug.LogFormat("<Simon Smothers> Keeping closest light to origin here at module ID {0}.", moduleId);
            _usedLight = closestLight;
        }
        else
            Destroy(bigLight.gameObject);
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
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} foobar> to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        yield return null;
    }
}
