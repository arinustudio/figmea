using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string name;

    public float regularSpeed;
    public float maxRegularSpeed;

    public float boostedSpeed;
    public float boostDuration;

    public float movementDuration;
    public float slideDuration;

}
