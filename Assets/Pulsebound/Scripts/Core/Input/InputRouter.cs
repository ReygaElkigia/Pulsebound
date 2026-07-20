using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pulsebound.Core.Input
{
    /// <summary>
    /// Bridges the New Input System to the game's event bus. The critical detail: every
    /// Bind press is stamped with <see cref="AudioSettings.dspTime"/> at the moment of the
    /// input callback — NOT read in Update — so judgment timing is frame-rate independent.
    ///
    /// Falls back to legacy Input polling if the Input System package is absent, so the
    /// project still compiles in a bare checkout.
    /// </summary>
    public sealed class InputRouter : MonoBehaviour
    {
        public bool Enabled { get; set; } = true;

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionReference bindAction;
        [SerializeField] private InputActionReference altBindAction;

        private InputAction _bind;
        private InputAction _altBind;
#endif

        private void Awake() => Services.Register<InputRouter>(this);

        private void OnDestroy()
        {
            if (Services.IsRegistered<InputRouter>() && Services.Get<InputRouter>() == this)
                Services.Unregister<InputRouter>();
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _bind = bindAction != null ? bindAction.action : DefaultBind();
            _altBind = altBindAction != null ? altBindAction.action : DefaultAltBind();
            _bind.performed += OnBindPerformed;
            _bind.canceled += OnBindCanceled;
            _altBind.performed += OnAltPerformed;
            _bind.Enable();
            _altBind.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (_bind != null)
            {
                _bind.performed -= OnBindPerformed;
                _bind.canceled -= OnBindCanceled;
                _bind.Disable();
            }
            if (_altBind != null)
            {
                _altBind.performed -= OnAltPerformed;
                _altBind.Disable();
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnBindPerformed(InputAction.CallbackContext ctx)
        {
            if (!Enabled) return;
            EventBus.Publish(new GameEvents.BindPressed(AudioSettings.dspTime, isAlt: false));
        }

        private void OnBindCanceled(InputAction.CallbackContext ctx)
        {
            if (!Enabled) return;
            EventBus.Publish(new GameEvents.BindReleased(AudioSettings.dspTime));
        }

        private void OnAltPerformed(InputAction.CallbackContext ctx)
        {
            if (!Enabled) return;
            EventBus.Publish(new GameEvents.BindPressed(AudioSettings.dspTime, isAlt: true));
        }

        private static InputAction DefaultBind()
        {
            var a = new InputAction("Bind", InputActionType.Button);
            a.AddBinding("<Keyboard>/space");
            a.AddBinding("<Mouse>/leftButton");
            a.AddBinding("<Keyboard>/j");
            a.AddBinding("<Gamepad>/buttonSouth");
            a.AddBinding("<Gamepad>/rightTrigger");
            return a;
        }

        private static InputAction DefaultAltBind()
        {
            var a = new InputAction("AltBind", InputActionType.Button);
            a.AddBinding("<Keyboard>/k");
            a.AddBinding("<Keyboard>/f");
            a.AddBinding("<Gamepad>/buttonWest");
            a.AddBinding("<Gamepad>/leftTrigger");
            return a;
        }
#else
        // Legacy fallback so the project compiles without the Input System package.
        private bool _wasDown;
        private void Update()
        {
            if (!Enabled) return;
            bool down = UnityEngine.Input.GetKey(KeyCode.Space) || UnityEngine.Input.GetMouseButton(0);
            if (down && !_wasDown)
                EventBus.Publish(new GameEvents.BindPressed(AudioSettings.dspTime, false));
            else if (!down && _wasDown)
                EventBus.Publish(new GameEvents.BindReleased(AudioSettings.dspTime));
            _wasDown = down;
        }
#endif
    }
}
