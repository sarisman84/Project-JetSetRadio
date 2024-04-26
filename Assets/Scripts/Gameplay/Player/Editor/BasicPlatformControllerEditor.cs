using ProjectJetSetRadio.Gameplay;
using UnityEditor;

namespace ProjectJetSetRadio.CustomEditors.Gameplay
{
    [CustomEditor(typeof(BasicPlatformerController))]
    public class BasicPlatformControllerEditor : Editor
    {
        private BasicPlatformerController _controller;
        private void OnEnable()
        {
            _controller = (BasicPlatformerController)target;
        }

        public override void OnInspectorGUI()
        {
            if (_controller.GetComponent<SkateController>() == null)
                base.OnInspectorGUI();
            else
                EditorGUILayout.HelpBox("Settings overriden by the Skate Controller", MessageType.Info);
        }
    }
}
