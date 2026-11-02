using UnityEditor;
using UnityEngine;
using Eko;

// Headless check that the campaign can be walked from the first line to the final choice:
// turned back at the checkpoint with a fever and let through after a dose, refused ZX without
// the money, and cured once it is paid for.
public static class StoryTest
{
    public static void Run()
    {
        Game.NewGame(); Story.Pos.Clear();
        foreach (var k in new[] { "mama", "baba", "sule", "tunde", "diesel", "plug", "desk", "gate", "bello", "ada", "vault" }) Story.Pos[k] = Vector2.zero;
        Story.Begin();
        int guard = 0; bool fail = false;
        while (Story.Cur != null && guard++ < 60)
        {
            var st = Story.Cur;
            Debug.Log("STEP " + Story.I + " kind=" + st.Kind + " target=" + st.Target + " cash=" + Game.Cash + " doses=" + Game.Doses + " obj=" + st.Obj);
            if (st.Kind == SK.Cutscene) GameUI.SkipDialogue();
            else if (st.Kind == SK.Talk)
            {
                if (st.Target == "gate")
                {
                    Game.Inf = 80f; int before = Story.I;
                    Story.Interact("gate"); GameUI.SkipDialogue();
                    if (Story.I != before) { Debug.LogError("Checkpoint let a feverish player through"); fail = true; break; }
                    while (!Game.IsCovered && Game.TakeDose()) { }
                    if (!Game.IsCovered) { Debug.LogError("Not enough doses to pass the checkpoint"); fail = true; break; }
                    Debug.Log("CHECKPOINT refused at fever, passed after a dose");
                }
                if (st.Target == "bello")
                {
                    int cash = Game.Cash; Game.Cash = 1000; int before = Story.I;
                    Story.Interact("bello"); GameUI.SkipDialogue();
                    if (Story.I != before) { Debug.LogError("Bello sold ZX without the money"); fail = true; break; }
                    Game.Cash = cash;
                    Debug.Log("BELLO refused when broke");
                }
                if (!Story.Interact(st.Target)) { Debug.LogError("Interact failed " + st.Target); fail = true; break; }
                GameUI.SkipDialogue();
                if (st.Target == "bello" && (Game.Bitten || Game.Inf > 0f)) { Debug.LogError("ZX did not cure"); fail = true; break; }
            }
            else if (st.Kind == SK.Choice) { Story.Interact(st.Target); Debug.Log("CHOICE reached"); break; }
        }
        if (Game.Cash < 0) { Debug.LogError("Cash went negative"); fail = true; }
        Debug.Log("STORY DONE index=" + Story.I + " pass=" + Game.HasPass + " cured=" + !Game.Bitten + " cash=" + Game.Cash + (fail ? " FAILED" : " OK"));
        EditorApplication.Exit(fail ? 1 : 0);
    }
}
