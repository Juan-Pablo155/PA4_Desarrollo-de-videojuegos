using UnityEngine;

namespace BosquePA4
{
    public class PA4Hud : MonoBehaviour
    {
        public PA4Game game;
        GUIStyle title, body, small, button;
        readonly Color cyan = new Color(.24f, .94f, .83f);
        void Styles()
        {
            if (title != null)
            {
                return;
            }

            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            title.normal.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                wordWrap = true
            };
            body.normal.textColor = Color.white;
            small = new GUIStyle(body)
            {
                fontSize = 17
            };
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold
            };
        }

        void Panel(Rect area, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        void OnGUI()
        {
            Styles();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (game.State == PA4Game.Phase.Playing)
            {
                DrawGameplay();
            }
            else
            {
                DrawMenu();
            }
        }

        void DrawGameplay()
        {
            Panel(new Rect(24, 24, 400, 130), new Color(.025f, .07f, .09f, .88f));
            GUI.Label(new Rect(44, 36, 370, 34), "CRISTALES   " + game.Collected + " / " + game.crystals.Length, body);
            for (int i = 0; i < 3; i++)
            {
                Panel(new Rect(45 + i * 54, 86, 44, 12), i < game.Health ? cyan : new Color(.25f, .28f, .3f));
            }

            GUI.Label(new Rect(44, 111, 370, 28), "WASD mover · Shift correr · Esc pausa", small);
            GUI.Label(new Rect(1100, 25, 150, 45), Mathf.FloorToInt(game.Remaining / 60).ToString("00") + ":" + Mathf.FloorToInt(game.Remaining % 60).ToString("00"), body);
            var target = game.NextTarget();
            if (target)
            {
                string goal = game.Collected == game.crystals.Length ? "REGRESA AL MIRADOR" : "BUSCA LOS CRISTALES";
                GUI.Label(new Rect(440, 26, 580, 35), goal, body);
                Vector3 local = game.player.transform.InverseTransformDirection(target.position - game.player.transform.position);
                string direction = local.z < 0 ? "Detrás de ti" : local.x > 3 ? "Hacia la derecha" : local.x < -3 ? "Hacia la izquierda" : "Frente a ti";
                GUI.Label(new Rect(440, 63, 580, 30), direction + "  /  " + Vector3.Distance(target.position, game.player.transform.position).ToString("0") + " m", small);
            }

            Panel(new Rect(637, 357, 6, 6), Color.white);
            if (game.Nearby)
            {
                Panel(new Rect(410, 520, 460, 95), new Color(.025f, .07f, .09f, .9f));
                GUI.Label(new Rect(435, 535, 425, 38), "Mantén E para recuperar el cristal", body);
                Panel(new Rect(435, 583, 410 * game.InteractionProgress, 8), cyan);
            }

            if (game.Threatened)
            {
                GUI.color = new Color(1, .45f, .32f);
                GUI.Label(new Rect(450, 160, 600, 40), "¡CENTINELA CERCA! Corre para escapar", body);
                GUI.color = Color.white;
            }

            if (!string.IsNullOrEmpty(game.Message))
            {
                Panel(new Rect(260, 642, 760, 48), new Color(.025f, .07f, .09f, .9f));
                GUI.Label(new Rect(280, 652, 730, 36), game.Message, small);
            }

            if (Time.unscaledTime < game.HurtUntil)
            {
                Panel(new Rect(0, 0, 1280, 720), new Color(.8f, .05f, .02f, .22f));
            }
        }

        void DrawMenu()
        {
            Panel(new Rect(0, 0, 1280, 720), new Color(.015f, .035f, .05f, .86f));
            GUI.color = cyan;
            GUI.Label(new Rect(110, 78, 1000, 35), "EXPEDICIÓN AL VALLE  /  PA4", body);
            GUI.color = Color.white;
            string heading;
            string text;
            string action;
            switch (game.State)
            {
                case PA4Game.Phase.Menu:
                    heading = "El eco de los cristales";
                    text = "Recupera los 5 cristales cian del valle y vuelve al mirador dorado.\n" + "Los centinelas patrullan la zona y te persiguen cuando te detectan.\n" + "Tienes 5 minutos y 3 puntos de vida.";
                    action = "COMENZAR";
                    break;
                case PA4Game.Phase.Paused:
                    heading = "Expedición en pausa";
                    text = "La partida está detenida. Continúa cuando estés listo.";
                    action = "CONTINUAR";
                    break;
                case PA4Game.Phase.Won:
                    heading = "Misión completada";
                    text = "Recuperaste los cristales y regresaste al mirador.\nTiempo restante: " + Mathf.CeilToInt(game.Remaining) + " segundos.";
                    action = "VOLVER A JUGAR";
                    break;
                default:
                    heading = "Expedición interrumpida";
                    text = game.Message;
                    action = "VOLVER A JUGAR";
                    break;
            }

            GUI.Label(new Rect(110, 145, 1000, 100), heading, title);
            GUI.Label(new Rect(112, 270, 1000, 130), text, body);
            GUI.Label(new Rect(112, 425, 1000, 65), "WASD: caminar   ·   Ratón: mirar   ·   Espacio: saltar\nShift: correr   ·   Mantener E: recoger   ·   Esc: pausar", small);
            if (GUI.Button(new Rect(110, 540, 350, 65), action, button))
            {
                if (game.State == PA4Game.Phase.Menu)
                {
                    game.Begin();
                }
                else if (game.State == PA4Game.Phase.Paused)
                {
                    game.TogglePause();
                }
                else
                {
                    game.Restart();
                }
            }

            if (game.State == PA4Game.Phase.Paused && GUI.Button(new Rect(485, 540, 280, 65), "REINICIAR", button))
            {
                game.Restart();
            }

            if (GUI.Button(new Rect(790, 540, 220, 65), "SALIR", button))
            {
                game.Exit();
            }
        }
    }
}
