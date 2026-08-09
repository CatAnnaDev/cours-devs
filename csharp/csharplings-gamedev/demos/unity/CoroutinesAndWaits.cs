using System.Collections;
using UnityEngine;

namespace Demos.Unity
{
    public sealed class CoroutinesAndWaits : MonoBehaviour
    {
        private static readonly WaitForSeconds OneTenth = new WaitForSeconds(0.1f);
        private static readonly WaitForSeconds OneSecond = new WaitForSeconds(1f);
        private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
        private static readonly WaitForFixedUpdate FixedStep = new WaitForFixedUpdate();

        private Coroutine _running;

        private void OnEnable()
        {
            _running = StartCoroutine(Pulse());
        }

        private void OnDisable()
        {
            if (_running != null)
                StopCoroutine(_running);

            _running = null;
        }

        private IEnumerator Wasteful()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator Pulse()
        {
            while (true)
            {
                yield return OneTenth;
            }
        }

        private IEnumerator FrameByFrame(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private IEnumerator AfterEverythingDrew()
        {
            yield return EndOfFrame;
        }

        private IEnumerator AlignedOnPhysics()
        {
            yield return FixedStep;
        }

        private IEnumerator IgnoringTimeScale(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private IEnumerator UntilCondition()
        {
            yield return new WaitUntil(() => transform.position.y > 10f);
        }

        private void Notes()
        {
            Debug.Log("Ce que coute une attente :");
            Debug.Log("  new WaitForSeconds(x) dans une boucle : un objet PAR tour, pour toujours");
            Debug.Log("  la meme instance en static readonly  : zero objet, timing identique");
            Debug.Log("  yield return null                    : gratuit, attend une frame");
            Debug.Log("Une WaitForSeconds est une DUREE, pas un etat : la reutiliser est sans danger,");
            Debug.Log("c'est le moteur qui garde le temps ecoule de son cote.");
            Debug.Log("Deux pieges de plus :");
            Debug.Log("  WaitForSeconds suit timeScale ; WaitForSecondsRealtime, non. Menus et sons");
            Debug.Log("  veulent la seconde version, sinon ils gelent quand le jeu est en pause.");
            Debug.Log("  desactiver le composant ARRETE ses coroutines et ne les relance pas :");
            Debug.Log("  d'ou le StartCoroutine dans OnEnable, pas dans Start.");
            Debug.Log("Chez Godot l'equivalent est 'await ToSignal(GetTree().CreateTimer(x), timeout)',");
            Debug.Log("qui alloue aussi, et qui n'est pas arrete par la desactivation du noeud.");
        }
    }
}
