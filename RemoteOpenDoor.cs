using BepInEx.Logging;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RemoteOpenDoor.Patch
{


    [HarmonyPatch(typeof(RemoteProp))]
    internal class RemotePropPatch
    {

        public static ManualLogSource Logger;

        private static RemotePropPatchMB remotePropPatchMB;

        public static float DoorActivationDistance = 15f;

        // Add the MonoBehaviour if you need to perform operations that require a MonoBehaviour context.
        public class RemotePropPatchMB : MonoBehaviour
        {
        }

        // Threshold distance within which the remote can trigger the door
        public static float doorActivationDistance = 15f;

        [HarmonyPatch(typeof(RemoteProp), "ItemActivate")]
        [HarmonyPrefix]
        public static bool ItemActivatePatch(ref RemoteProp __instance, ref AudioSource ___remoteAudio, bool used, bool buttonDown = true)
        {
            if ((UnityEngine.Object)(object)remotePropPatchMB == (UnityEngine.Object)null)
            {
                GameObject gameObject = new GameObject("RemotePropMB");
                remotePropPatchMB = gameObject.AddComponent<RemotePropPatchMB>();
            }

            Transform playerTransform = ((Component)__instance).transform;
            AudioSource audioSource = ___remoteAudio;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(___remoteAudio.clip);
            }
            else
            {
                Logger?.LogWarning("AudioSource is null in RemoteProp");
            }

            // Find and interact with doors
            TerminalAccessibleObject[] doors = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
            if (doors.Length == 0)
            {
                Logger?.LogInfo("No doors found in the scene to interact with.");
            }

            // Find all doors within the activation distance
            var doorsWithinDistance = doors
                .Select(door => new { Door = door, Distance = Vector3.Distance(playerTransform.position, door.transform.position) })
                .Where(doorInfo => doorInfo.Distance <= DoorActivationDistance)
                .OrderBy(doorInfo => doorInfo.Distance)
                .ToList();

            if (doorsWithinDistance.Count > 0)
            {
                bool GetIsPoweredOn(TerminalAccessibleObject door)
                {
                    FieldInfo fieldInfo = typeof(TerminalAccessibleObject).GetField("isPoweredOn", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)fieldInfo?.GetValue(door);
                }
                // Select the closest door
                var closestDoorInfo = doorsWithinDistance.First();
                if (GetIsPoweredOn(closestDoorInfo.Door)) // Use the isPoweredOn property
                {
                    Logger?.LogInfo($"Activating closest door {closestDoorInfo.Door.gameObject.name} at distance {closestDoorInfo.Distance}");
                    closestDoorInfo.Door.SetDoorToggleLocalClient();

                    // Update the server about the door status
                    bool doorStatus = closestDoorInfo.Door.GetComponent<AnimatedObjectTrigger>().boolValue;
                    closestDoorInfo.Door.SetDoorOpenServerRpc(doorStatus);
                }
                else
                {
                    HUDManager.Instance.DisplayTip("Door Unpowered", "The door cannot be opened as it is unpowered.");
                }
            }

            return false; // To prevent the original method from executing
        }
    }
}
