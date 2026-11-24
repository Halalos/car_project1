using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cars
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        void Start() { SceneManager.LoadScene("MainMenu"); }
    }
}
