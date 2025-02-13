using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance => instance;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else
        {
            if (transform.parent.childCount == 1)
            {
                Destroy(transform.parent);
            }
            else
            {
                Destroy(gameObject);
            }
        }


        if (transform.parent != null && transform.root != null) 
        {
            DontDestroyOnLoad(this.transform.root.gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject); 
        }
    }
}
