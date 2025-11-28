// using UnityEngine;
// // We MUST add this line to access the tvOS-specific remote features
// using UnityEngine.Apple.TV; 

// public class tvOSSetup : MonoBehaviour
// {
//     // This attribute makes this function run automatically
//     // as soon as the game launches, before any scene loads.
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//     private static void ConfiguretvOS()
//     {
//         // This is the line you found.
//         // It tells tvOS not to quit on a short menu press.
//         UnityEngine.tvOS.Remote.allowExitToHome = false;

//         Debug.Log("--- tvOSSetup: allowExitToHome set to FALSE. ---");
//     }
// }