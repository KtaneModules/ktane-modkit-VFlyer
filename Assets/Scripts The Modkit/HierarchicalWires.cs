using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

class HierarchicalWires : Puzzle
{
    int[][] cutOrder = new int[][] {
        new int[] { 3, 4, 5, 2, 1 },
        new int[] { 3, 2, 5, 1, 4 },
        new int[] { 5, 1, 2, 4, 3 },
        new int[] { 4, 2, 1, 5, 3 },
        new int[] { 1, 3, 2, 4, 5 },
        new int[] { 2, 1, 4, 5, 3 },
        new int[] { 3, 4, 2, 5, 1 },
        new int[] { 2, 3, 1, 4, 5 },
        new int[] { 1, 2, 3, 5, 4 },
        new int[] { 2, 5, 3, 4, 1 },
        new int[] { 4, 3, 5, 2, 1 },
        new int[] { 5, 4, 2, 3, 1 },
        new int[] { 2, 5, 4, 3, 1 },
        new int[] { 3, 1, 4, 5, 2 },
        new int[] { 5, 1, 4, 3, 2 },
        new int[] { 4, 2, 5, 1, 3 },
        new int[] { 5, 4, 1, 2, 3 },
        new int[] { 5, 2, 3, 4, 1 },
        new int[] { 5, 4, 3, 2, 1 },
        new int[] { 4, 3, 5, 1, 2 },
        new int[] { 2, 5, 1, 3, 4 },
        new int[] { 1, 2, 4, 5, 3 },
        new int[] { 3, 1, 5, 4, 2 },
        new int[] { 3, 5, 2, 4, 1 },
        new int[] { 2, 5, 3, 4, 1 },
        new int[] { 2, 4, 1, 5, 3 },
        new int[] { 5, 1, 2, 3, 4 },
        new int[] { 2, 3, 5, 4, 1 },
        new int[] { 3, 5, 1, 2, 4 },
        new int[] { 4, 1, 2, 3, 5 },
        new int[] { 3, 2, 4, 1, 5 }
    };

    int stage = 0;
    List<int> toCut;
    List<int> cut;

    public HierarchicalWires(Modkit module, int moduleId, ComponentInfo info) : base(module, moduleId, info)
    {
        Debug.LogFormat("[The Modkit #{0}] Solving Hierarchical Wires. Symbols present: {1}. LEDs: {2}.", moduleId, info.GetSymbols(), info.LED.Select(x => ComponentInfo.COLORNAMES[x]).Join(", "));

        CalcSolution();
    }

    public override void OnWireCut(int wire)
    {
        if(module.IsAnimating())
            return;

        module.audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, module.transform);
		module.CutWire(wire);

        if(module.IsSolved())
            return;

        if(!module.CheckValidComponents())
        {
		    Debug.LogFormat("[The Modkit #{0}] Strike! Wire {1} was cut when the component selection was [ {2} ] instead of [ {3} ].", moduleId, wire + 1, module.GetOnComponents(), module.GetTargetComponents());
            module.CauseStrike();
            module.RegenWires();
            Debug.LogFormat("[The Modkit #{0}] Wires present: {1}.", moduleId, info.GetWireNames());
            CalcSolution();
            return;
        }

        module.StartSolve();
        Debug.LogFormat("[The Modkit #{0}] Cutting wire {1}.", moduleId, wire + 1);
        cut.Add(wire);
    }

    public override void OnSymbolPress(int symbol)
    {
        if(module.IsAnimating())
            return;

        module.audioSelf.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
        module.symbols[symbol].GetComponentInChildren<KMSelectable>().AddInteractionPunch(0.5f);
    
        if(module.IsSolved())
            return;

        if(!module.CheckValidComponents())
        {
		    Debug.LogFormat("[The Modkit #{0}] Strike! Symbol {1} was pressed when the component selection was [ {2} ] instead of [ {3} ].", moduleId, symbol + 1, module.GetOnComponents(), module.GetTargetComponents());
            module.CauseStrike();
            return;
        }

        module.StartSolve();

        if(symbol != stage)
        {
		    Debug.LogFormat("[The Modkit #{0}] Strike! Incorrectly pressed symbol {1} on stage {2}.", moduleId, symbol + 1, stage + 1);
            module.CauseStrike();
            module.RegenWires();
            CalcSolution();
            return;
        }

        if(!toCut.SequenceEqual(cut))
        {
            Debug.LogFormat("[The Modkit #{0}] Strike! Incorrectly cutted wires [ {1} ], in that order, when wires [ {2} ] were expected, in that order.", moduleId, cut.Any() ? cut.Select(x => x + 1).Join(", ") : "none", toCut.Any() ? toCut.Select(x => x + 1).Join(", ") : "none");
            module.CauseStrike();
            module.RegenWires();
            CalcSolution();
            return;
        }
        else
        {
            Debug.LogFormat("[The Modkit #{0}] Correctly cut wires [ {1} ], in that order.", moduleId, toCut.Any() ? toCut.Select(x => x + 1).Join(", ") : "none");
            module.symbols[symbol].transform.Find("Key_TL").Find("LED").GetComponentInChildren<Renderer>().material = module.keyLightMats[1];
            stage++;
        
            if(stage >= 3)
            {
                Debug.LogFormat("[The Modkit #{0}] Module solved.", moduleId);
                module.Solve();
            }
            else
            {
                module.RegenWires();
                CalcSolution();
            }
        }
    }

    void CalcSolution()
    {
        Debug.LogFormat("[The Modkit #{0}] Wires present: {1}.", moduleId, info.GetWireNames());
        Debug.LogFormat("[The Modkit #{0}] Stage {1}'s order: {2}.", moduleId, stage + 1, cutOrder[info.symbols[stage]].Join(", "));
        
        toCut = new List<int>();
        cut = new List<int>();

        for(int i = 0; i < cutOrder[info.symbols[stage]].Length; i++)
        {
            int color1 = info.wires[cutOrder[info.symbols[stage]][i] - 1] / 10;
            int color2 = info.wires[cutOrder[info.symbols[stage]][i] - 1] % 10;

            if(color1 == info.LED[stage] || color2 == info.LED[stage])
                toCut.Add(cutOrder[info.symbols[stage]][i] - 1);
        }

        Debug.LogFormat("[The Modkit #{0}] Wire cut order: [ {1} ].", moduleId, toCut.Any() ? toCut.Select(x => x + 1).Join(", ") : "none");
    }
}