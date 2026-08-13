using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace Game
{
    /// <summary>A button that plays a configured sound whenever its click event is invoked.</summary>
    public class ClickSoundButton : Button
    {
        [ValueDropdown("@Game.ConstantManager.GetSoundNames()")]
        public string buttonClickSound;

        protected override void OnEnable()
        {
            base.OnEnable();
            onClick.AddListener(PlayClickSound);
        }

        protected override void OnDisable()
        {
            onClick.RemoveListener(PlayClickSound);
            base.OnDisable();
        }

        private void PlayClickSound()
        {
            if (!string.IsNullOrEmpty(buttonClickSound) && SoundManager.Instance != null)
                SoundManager.Instance.Play(buttonClickSound);
        }
    }
}
