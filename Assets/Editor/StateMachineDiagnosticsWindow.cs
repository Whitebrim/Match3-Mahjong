using Core.Infrastructure.StateMachine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Editor
{
    public class StateMachineDiagnosticsWindow : OdinEditorWindow
    {
        private IObjectResolver _resolver;
        private GameStateMachine _stateMachine;
        private string _currentStateName;

        [MenuItem("Window/State Machine Diagnostics")]
        public static void ShowWindow()
        {
            GetWindow<StateMachineDiagnosticsWindow>("State Machine Diagnostics");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.update += UpdateState;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= UpdateState;
        }

        private void UpdateState()
        {
            if (!Application.isPlaying) return;
            
            _resolver ??= FindFirstObjectByType<LifetimeScope>()?.Container;

            if (_resolver != null && _stateMachine == null)
            {
                _stateMachine = _resolver.Resolve<GameStateMachine>();
            }

            if (_stateMachine == null) return;
            _currentStateName = _stateMachine.CurrentState.GetType().Name;
            Repaint();
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();
            if (!Application.isPlaying)
            {
                _stateMachine = null;
                GUILayout.Label("Enter Play Mode");
                return;
            }
            
            if (_resolver == null)
            {
                GUILayout.Label("LifetimeScope not found");
                return;
            }

            if (_stateMachine == null)
            {
                GUILayout.Label("State Machine not found");
                return;
            }

            var stateStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                //alignment = TextAnchor.UpperCenter
            };

            GUILayout.Label(_currentStateName, stateStyle);
        }

        protected override object GetTarget()
        {
            if (_stateMachine == null) return this;
            return _stateMachine.CurrentState;
        }
    }
}