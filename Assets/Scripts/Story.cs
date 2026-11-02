using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: the campaign. Lagos in the early days of Eko Fever. Dayo is infected and hiding it
// from the Taskforce, working odd jobs to raise money for ZX, the dose that stops it for good.
namespace Eko
{
    public enum SK { Cutscene, Talk, Choice }

    public class Step
    {
        public string Card;          // chapter title card shown when the step begins
        public string Obj;           // the objective line in the HUD
        public string Target;        // id of the NPC or prop to reach (also the waypoint)
        public SK Kind = SK.Talk;
        public string[] Lines;       // "Speaker|text"
        public string[] Options;     // for Choice
        public System.Func<string[]> Check; // lines to play instead when you are turned away
        public System.Action Done;
        public bool Hostile;         // guards are watching
        public Vector2? Spawn;       // where you wake if you are caught
    }

    public static class Story
    {
        public static List<Step> S;
        public static int I;
        public static Vector2 LastSafe;
        public static readonly Dictionary<string, Vector2> Pos = new Dictionary<string, Vector2>();

        public static readonly Dictionary<string, Vector2> Via = new Dictionary<string, Vector2>(); // indoor targets: the street door

        public static Step Cur => S != null && I < S.Count ? S[I] : null;
        public static bool Hostile => Cur != null && Cur.Hostile;

        /// Where the waypoint points. Indoor targets point at the building's door until you are inside,
        /// and from inside a building an outdoor target points at the way out.
        public static Vector2? TargetPos
        {
            get
            {
                if (Cur == null || Cur.Target == null || !Pos.TryGetValue(Cur.Target, out var p)) return null;
                var here = Player.I != null ? Interior.At(Player.I.Pos) : null;
                var there = Interior.At(new Vector3(p.x, 0, p.y));
                if (here == there) return p;
                if (here != null) return new Vector2(here.Exit.x, here.Exit.z);
                return Via.TryGetValue(Cur.Target, out var door) ? door : p;
            }
        }

        /// 1..5. Drives the time of day, the news and how many Taskforce patrols are out.
        public static int Chapter => I <= 4 ? 1 : I <= 7 ? 2 : I <= 10 ? 3 : I == 11 ? 4 : 5;

        public static int Patrols => new[] { 0, 5, 8, 10, 12, 12 }[Chapter];

        static bool babaDash, tundeDash;

        public static void Begin()
        {
            I = 0; babaDash = tundeDash = false;
            S = new List<Step>
            {
                // 0
                new Step
                {
                    Kind = SK.Cutscene, Card = "EKO FEVER",
                    Lines = new[]
                    {
                        "Narrator|Lagos. They call it Eko Fever. The government says it is under control. The Taskforce says the same thing, and takes anyone with dark veins to the quarantine camp in Ikorodu.",
                        "Narrator|Nobody has come back from the camp.",
                        "Narrator|You are Dayo, a danfo driver. Four days ago a coughing passenger bit you in a fight over change. Your veins have started to show.",
                        "Ada|Dayo, it's Ada. Listen to me. The Suppressant they sell on the street only hides the veins for a few minutes. It does not stop anything.",
                        "Ada|There is a long-acting dose called ZX. Dr Bello sells it from the Island Hospital in Lekki. It costs twenty-five thousand naira. Raise the money and get across to me.",
                        "Ada|And Dayo, keep your fever down. If the Taskforce sees your veins, they take you. Please.",
                        "Dayo|Twenty-five thousand. I have three. Okay. Okay.",
                    },
                },
                // 1
                new Step
                {
                    Card = "CHAPTER 1 · HUSTLE", Obj = "Find Mama Put at her buka, down the market street", Target = "mama",
                    Lines = new[]
                    {
                        "Mama Put|Dayo, you look like death. Sit. Let me see your arm... Jesus. Pull your sleeve down.",
                        "Mama Put|Take these two doses of Suppressant. When the bar at the bottom of your screen passes the white line, your veins show. Press Q and the dose pushes it back.",
                        "Mama Put|Twenty-five thousand for ZX? Then you need work. Baba Ijebu at his pools kiosk always has jobs for people who don't ask questions.",
                        "Mama Put|Taskforce walk these streets in orange vests. If they see your veins, run, and don't stop till you lose them. Or jump in a car. Press F next to one.",
                    },
                    Done = () => { Game.Doses += 2; GameUI.Toast("Mama Put gave you 2 doses. Q takes one when your fever passes the white line."); },
                },
                // 2
                new Step
                {
                    Obj = "Find Baba Ijebu at his pools kiosk in north Mushin", Target = "baba",
                    Lines = new[]
                    {
                        "Baba Ijebu|A job? Hm. Sule at Kilimanjaro Bar owes me twelve thousand and he has been dodging my boys for a week.",
                        "Baba Ijebu|The bar is on the market street, west of Mama Put. Walk in, collect my money, bring it here. You get your share when I see it.",
                        "Baba Ijebu|And Dayo? Taskforce like to drink there. Keep your sleeves down.",
                    },
                },
                // 3
                new Step
                {
                    Obj = "Collect Baba's money from Sule inside Kilimanjaro Bar", Target = "sule",
                    Lines = new[]
                    {
                        "Sule|Baba sent you? That old vulture. Look around you, Dayo. Half my customers are hiding a fever, the other half are hunting them.",
                        "Sule|Here. Twelve thousand. Tell him the next time he wants money, he can come and drink like a gentleman.",
                        "Sule|And take advice from a barman. The Taskforce don't care who you are. They get paid per head at that camp.",
                    },
                    Done = () => GameUI.Toast("You have Baba's envelope."),
                },
                // 4
                new Step
                {
                    Obj = "Bring Sule's money back to Baba Ijebu", Target = "baba",
                    Lines = new[]
                    {
                        "Baba Ijebu|Ah! He paid. You have a trustworthy face, Dayo. That is rare.",
                        "Baba Ijebu|Here is your cut. And if you want real money, go and see Tunde. He is DJing an owambe in Yaba, east of here, and his generator is always hungry.",
                    },
                    Done = () => { Game.Cash += 12000; GameUI.Toast("+" + Game.Naira(12000)); },
                },
                // 5
                new Step
                {
                    Card = "CHAPTER 2 · THE OWAMBE", Obj = "Find Tunde at the owambe in Yaba. Follow the music", Target = "tunde",
                    Lines = new[]
                    {
                        "Tunde|Dayo! My guy! The city is falling apart and we are still dancing. That is Lagos for you.",
                        "Tunde|You need money? I need diesel. The generator dies, the party dies. There are black-market jerrycans waiting for me at the depot in Apapa, south of here.",
                        "Tunde|Bring them back and I will pay you twenty-five thousand. Take a car, it is far.",
                    },
                },
                // 6
                new Step
                {
                    Obj = "Pick up Tunde's diesel at the black-market depot in Apapa", Target = "diesel",
                    Lines = new[] { "Dayo|Four jerrycans. That boy's generator drinks better than I eat." },
                    Done = () => GameUI.Toast("You have the diesel. Back to the owambe."),
                },
                // 7
                new Step
                {
                    Obj = "Bring the diesel back to Tunde at the owambe", Target = "tunde",
                    Lines = new[]
                    {
                        "Tunde|The diesel! The party lives! Twenty-five thousand, as promised. Dance small before you go.",
                        "Tunde|You're going to the Island for ZX? Then hear me. The Lekki checkpoint only lets through people with a work permit.",
                        "Tunde|The Plug in Ajegunle makes them. Far south-west. Press M for the map.",
                    },
                    Done = () => { Game.Cash += 25000; GameUI.Toast("+" + Game.Naira(25000)); },
                },
                // 8
                new Step
                {
                    Card = "CHAPTER 3 · PAPERS", Obj = "Find the Plug on a corner in Ajegunle", Target = "plug",
                    Lines = new[]
                    {
                        "The Plug|A work permit for Lekki? Five thousand, and it is a gift at that price.",
                        "The Plug|But paper without a stamp is toilet paper. The stamp is on a desk in the Apapa Port Yard office, down by the water.",
                        "The Plug|Guards patrol the yard. Their eyes show as cones on the ground, and they cannot see through containers. Hold C to sneak. Leave the car outside.",
                    },
                    Done = () => { Game.Cash -= 5000; GameUI.Toast("-" + Game.Naira(5000) + " to the Plug."); },
                },
                // 9
                new Step
                {
                    Obj = "Sneak through the Apapa Port Yard to the stamp desk by the office. C to sneak", Target = "desk", Hostile = true,
                    Spawn = new Vector2(225f, -223f),
                    Lines = new[] { "Dayo|The stamp. Now get out before the guards come back around." },
                },
                // 10
                new Step
                {
                    Obj = "Bring the stamp back to the Plug in Ajegunle", Target = "plug",
                    Lines = new[]
                    {
                        "The Plug|Beautiful. As official as the Governor's wife.",
                        "The Plug|At the checkpoint they will check your hands. Take a dose before you get there. If your veins show, permit or no permit, you go to the camp.",
                    },
                    Done = () => { Game.HasPass = true; GameUI.Toast("You have a work permit. Cross the Lekki Link Bridge."); },
                },
                // 11
                new Step
                {
                    Card = "CHAPTER 4 · THE BRIDGE", Obj = "Cross the Lekki Link Bridge to the checkpoint at the Lekki Gardens gate. Keep your fever below the line", Target = "gate",
                    Check = () => Game.IsCovered ? null : new[]
                    {
                        "Soldier|Stop there. Hands where I can see them.",
                        "Soldier|... Your veins, my friend. Back away from my checkpoint, slowly. If you are still here in ten seconds, I am calling the Taskforce.",
                        "Dayo|(Take a dose with Q to bring the fever down, then try again.)",
                    },
                    Lines = new[]
                    {
                        "Soldier|Stop there. Hands. Eyes. Permit.",
                        "Soldier|Clean, and the stamp is real. Go on. The hospital is inside the estate, north side. Curfew is curfew.",
                    },
                    Done = () => { Game.Member = Game.Lekki; GameUI.Toast("The checkpoint lifts. You are on the Island."); },
                },
                // 12
                new Step
                {
                    Card = "CHAPTER 5 · THE ISLAND", Obj = "Buy ZX from Dr Bello inside the Island Hospital (" + Game.Naira(Game.ZxPrice) + ")", Target = "bello",
                    Check = () => Game.Cash >= Game.ZxPrice ? null : new[]
                    {
                        "Dr Bello|ZX is " + Game.Naira(Game.ZxPrice) + ". Not one kobo less. Come back when you have it.",
                        "Dayo|(Baba Ijebu and Tunde might spot you something. Don't spend it all on street doses.)",
                    },
                    Lines = new[]
                    {
                        "Dr Bello|Twenty-five thousand. Count it on the desk, slowly. Good.",
                        "Dr Bello|Roll up your sleeve. One injection, and the fever stops climbing. You will feel it in an hour.",
                        "Dr Bello|And Mr Dayo. You did not get this from me.",
                    },
                    Done = () => { Game.Cash -= Game.ZxPrice; Game.Cure(); GameUI.Toast("ZX injected. The fever is gone, and it is not coming back."); },
                },
                // 13
                new Step
                {
                    Obj = "Find Ada in the hospital ward", Target = "ada",
                    Lines = new[]
                    {
                        "Ada|Dayo! You made it! Your arm... it's clear. You bought ZX from Bello?",
                        "Ada|Then you should know where it comes from. ZX is not his. It is the government's stock: a cure, enough for the whole city. It came in on the first day.",
                        "Ada|Commissioner Adeyemi keeps it in a vault under his villa, here in the estate. He sells it a dose at a time, and the Taskforce fills the camp with everyone who can't pay.",
                        "Ada|There are thousands of people in Ikorodu, Dayo. Get into that vault. Whatever is in there, Lagos deserves to know.",
                    },
                },
                // 14
                new Step
                {
                    Card = "CHAPTER 6 · THE VAULT", Obj = "Sneak past the Commissioner's guards to the vault under the villa", Target = "vault", Hostile = true,
                    Spawn = new Vector2(765f, 84f),
                    Lines = new[]
                    {
                        "Dayo|Crates. Hundreds of them. All ZX.",
                        "Chief Adeyemi|Put it down, driver.",
                        "Chief Adeyemi|Do you know what happens when everyone is cured? The fear ends, and so does the money. Fear keeps this city in order.",
                        "Chief Adeyemi|Take a crate and walk away, rich. Or open your mouth and join the others in Ikorodu.",
                    },
                },
                // 15
                new Step
                {
                    Kind = SK.Choice, Obj = "Decide", Target = "vault",
                    Options = new[]
                    {
                        "Take his radio. Tell all of Lagos where the cure is.",
                        "Take the crate, take Ada, and disappear.",
                    },
                },
            };
            Enter();
        }

        static void Enter()
        {
            var st = Cur; if (st == null) return;
            if (st.Card != null) GameUI.Card(st.Card);
            if (st.Kind == SK.Cutscene) GameUI.PlayLines(st.Lines, () => { st.Done?.Invoke(); Advance(); });
        }

        public static void Advance()
        {
            I++;
            if (Player.I != null) LastSafe = new Vector2(Player.I.Pos.x, Player.I.Pos.z);
            Enter();
        }

        /// The player pressed E on something with an id.
        public static bool Interact(string id)
        {
            var st = Cur;
            if (st == null) return false;
            if (st.Kind == SK.Talk && st.Target == id)
            {
                var refused = st.Check?.Invoke();
                if (refused != null) { GameUI.PlayLines(refused, null); return true; }
                GameUI.PlayLines(st.Lines, () => { st.Done?.Invoke(); Advance(); });
                return true;
            }
            if (st.Kind == SK.Choice && st.Target == id) { GameUI.OpenChoice(st.Options, Choose); return true; }
            return false;
        }

        /// What someone says when they are not part of the current mission. Mama Put and the Plug sell doses.
        public static string Bark(string id)
        {
            switch (id)
            {
                case "mama":
                case "plug":
                    string who = id == "mama" ? "Mama Put" : "The Plug";
                    if (!Game.Bitten) return who + ": Your arm is clear! Whatever you did, don't tell me.";
                    if (Game.Doses >= 5) return who + ": You are carrying enough already.";
                    if (Game.BuyDose()) return who + ": One dose, " + Game.Naira(Game.DosePrice) + ". God help you. (Doses: " + Game.Doses + ")";
                    return who + ": A dose is " + Game.Naira(Game.DosePrice) + ". Come back with money.";
                case "baba":
                    if (I > 4 && !babaDash) { babaDash = true; Game.Cash += 5000; return "Baba Ijebu: You did good work. Here, " + Game.Naira(5000) + ". Don't tell my wife."; }
                    return "Baba Ijebu: The pools are slow today. Everything is slow.";
                case "tunde":
                    if (I > 7 && !tundeDash) { tundeDash = true; Game.Cash += 5000; return "Tunde: Take " + Game.Naira(5000) + " extra. For the road, my guy."; }
                    return "Tunde: Go, go! Before the checkpoint closes!";
                case "sule": return "Sule: Drink something or go.";
                case "bello": return "Dr Bello: Appointments only.";
                case "ada": return "Ada: Not yet. Stay out of sight.";
                case "gate": return "Soldier: Checkpoint. Nobody crosses without a permit.";
                default: return "Not now.";
            }
        }

        static void Choose(int i)
        {
            Game.Won = true;
            if (i == 0)
            {
                Game.EndingTitle = "THE WALLS FALL";
                Game.Ending = "Dayo takes the Commissioner's radio and reads the vault's inventory live on Lockdown News, 90.5. By dawn the bridge is full of people coming for the cure, and the soldiers do not stop them. The camp in Ikorodu opens a week later. Lagos is not saved in one night. But for the first time since the fever, people open their doors.";
            }
            else
            {
                Game.EndingTitle = "ONE CRATE";
                Game.Ending = "Dayo takes the crate and Ada's hand, and they leave Lagos before dawn. They live well. Behind them the Taskforce keeps filling the camp, the radio keeps playing, and Commissioner Adeyemi keeps selling hope one dose at a time. Some nights Dayo still hears the whistles, and he does not turn around.";
            }
            GameUI.EndGame();
        }

        /// The Taskforce got you, or the fever dropped you in the street and they collected you.
        public static void Arrested(bool collapsed = false)
        {
            Game.EndingTitle = "ARRESTED";
            Game.Ending = collapsed
                ? "The fever takes your legs out from under you in the middle of the road. When you wake, you are in the back of a Taskforce van, and the next stop is the quarantine camp in Ikorodu."
                : "The Taskforce pins you to the ground and checks your arm. The veins are plain to see. You are thrown into the back of a van, and the next stop is the quarantine camp in Ikorodu.";
            GameUI.Arrest();
        }

        /// Back to the last checkpoint with the fever under control.
        public static void Retry()
        {
            Game.Inf = Mathf.Min(Game.Inf, 30f); if (Game.Bitten && Game.Doses < 1) Game.Doses = 1;
            Game.Ending = Game.EndingTitle = null;
            if (Player.I != null) Player.I.Teleport(Cur != null && Cur.Spawn.HasValue ? Cur.Spawn.Value : LastSafe);
        }

        // Caught by a guard: wake at the last safe place. Nothing is lost.
        public static void Caught(string msg)
        {
            if (Player.I == null) return;
            var st = Cur;
            Vector2 to = st != null && st.Spawn.HasValue ? st.Spawn.Value : LastSafe;
            Player.I.Teleport(to);
            GameUI.Toast(msg, true); GameUI.Flash(new Color(0.8f, 0.05f, 0.05f));
        }

        public static void Drowned()
        {
            if (Player.I == null) return;
            Player.I.Teleport(Cur != null && Cur.Spawn.HasValue ? Cur.Spawn.Value : LastSafe);
            GameUI.Toast("Dayo cannot swim. You crawl back to the last safe spot.", true);
            GameUI.Flash(new Color(0.1f, 0.3f, 0.6f));
        }

    }
}
