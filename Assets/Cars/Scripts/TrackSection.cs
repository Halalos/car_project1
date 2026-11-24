using UnityEngine;

namespace Cars
{
    // Each direct child is a short road slice or decoration. Its authored local Z is its path station.
    public sealed class TrackSection : MonoBehaviour
    {
        public float StartDistance { get; private set; }
        Vector3[] offsets;
        Quaternion[] rotations;
        Transform[] pieces;
        public void Capture()
        {
            pieces = new Transform[transform.childCount];
            offsets = new Vector3[pieces.Length]; rotations = new Quaternion[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            { pieces[i] = transform.GetChild(i); offsets[i] = pieces[i].localPosition; rotations[i] = pieces[i].localRotation; }
        }
        public void Place(TrackSystem track, float distance)
        {
            StartDistance = distance;
            for (int i = 0; i < pieces.Length; i++)
            {
                var offset = offsets[i];
                float station = distance + offset.z;
                pieces[i].SetPositionAndRotation(track.Point(station, offset.x, offset.y), track.Direction(station) * rotations[i]);
            }
        }
    }
}
