using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// シーンに1つだけ置く MonoBehaviour の基礎クラス
    /// 2つ目が作られたら（同じシーンを読み込み直したときなど）、後から来た方を消す
    /// </summary>
    /// <typeparam name="T">継承したクラス自身</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        [Tooltip("シーンを切り替えても消さないかどうか")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        /// <summary>今あるインスタンス。まだなければ null</summary>
        public static T Instance { get; private set; }

        /// <summary>インスタンスがあるかどうか</summary>
        public static bool HasInstance => Instance != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            if (dontDestroyOnLoad)
            {
                // DontDestroyOnLoad はルートのオブジェクトにしか効かない
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            OnAwake();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            OnRelease();
            Instance = null;
        }

        /// <summary>
        /// インスタンスとして登録されたときに呼ばれる。Awake の代わりに使う
        /// 後から来て消されるインスタンスでは呼ばれない
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        /// <summary>
        /// インスタンスが破棄されるときに呼ばれる。OnDestroy の代わりに使う
        /// </summary>
        protected virtual void OnRelease()
        {
        }
    }
}
