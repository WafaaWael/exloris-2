using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.UI
{
	static class UnityEvent2Menu
	{
		private static DefaultControls.Resources s_StandardResources;

		private const string kUILayerName = "UI";
		private const string kStandardSpritePath = "UI/Skin/UISprite.psd";
		private const string kBackgroundSpritePath = "UI/Skin/Background.psd";
		private const string kInputFieldBackgroundPath = "UI/Skin/InputFieldBackground.psd";
		private const string kKnobPath = "UI/Skin/Knob.psd";
		private const string kCheckmarkPath = "UI/Skin/Checkmark.psd";
		private const string kDropdownArrowPath = "UI/Skin/DropdownArrow.psd";
		private const string kMaskPath = "UI/Skin/UIMask.psd";

		private const float kWidth = 160f;
		private const float kThickHeight = 30f;
		private const float kThinHeight = 20f;

		private static Vector2 s_ThickElementSize = new Vector2(kWidth, kThickHeight);
		private static Vector2 s_ThinElementSize = new Vector2(kWidth, kThinHeight);

		private static Color s_TextColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
		private static Color s_DefaultSelectableColor = new Color(1f, 1f, 1f, 1f);
		private static Color s_PanelColor = new Color(1f, 1f, 1f, 0.392f);

		private static DefaultControls.Resources GetStandardResources()
		{
			if (s_StandardResources.standard == null)
			{
				s_StandardResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
				s_StandardResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpritePath);
				s_StandardResources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>(kInputFieldBackgroundPath);
				s_StandardResources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
				s_StandardResources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>(kCheckmarkPath);
				s_StandardResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>(kDropdownArrowPath);
				s_StandardResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>(kMaskPath);
			}
			return s_StandardResources;
		}

		[MenuItem("GameObject/UI/Button 2.0", false, 2031)]
		public static void AddButton(MenuCommand menuCommand)
		{
			GameObject go = CreateButton(GetStandardResources());

			PlaceUIElementRoot(go, menuCommand);
		}

		[MenuItem("GameObject/UI/Toggle 2.0", false, 2032)]
		public static void AddToggle(MenuCommand menuCommand)
		{
			GameObject go = CreateToggle(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		[MenuItem("GameObject/UI/Slider 2.0", false, 2034)]
		public static void AddSlider(MenuCommand menuCommand)
		{
			GameObject go = CreateSlider(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		[MenuItem("GameObject/UI/Scrollbar 2.0", false, 2035)]
		public static void AddScrollbar(MenuCommand menuCommand)
		{
			GameObject go = CreateScrollbar(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		// More advanced controls below

		[MenuItem("GameObject/UI/Dropdown 2.0", false, 2036)]
		public static void AddDropdown(MenuCommand menuCommand)
		{
			GameObject go = CreateDropdown(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		[MenuItem("GameObject/UI/Input Field 2.0", false, 2037)]
		public static void AddInputField(MenuCommand menuCommand)
		{
			GameObject go = CreateInputField(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		[MenuItem("GameObject/UI/Scroll View 2.0", false, 2063)]
		public static void AddScrollView(MenuCommand menuCommand)
		{
			GameObject go = CreateScrollView(GetStandardResources());
			PlaceUIElementRoot(go, menuCommand);
		}

		private static void PlaceUIElementRoot(GameObject element, MenuCommand menuCommand)
		{
			GameObject parent = menuCommand.context as GameObject;
			if (parent == null || parent.GetComponentInParent<Canvas>() == null)
				parent = GetOrCreateCanvasGameObject();

			string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent.transform, element.name);
			element.name = uniqueName;
			Undo.RegisterCreatedObjectUndo(element, "Create " + element.name);
			Undo.SetTransformParent(element.transform, parent.transform, "Parent " + element.name);
			GameObjectUtility.SetParentAndAlign(element, parent);
			if (parent != menuCommand.context) // not a context click, so center in sceneview
				SetPositionVisibleinSceneView(parent.GetComponent<RectTransform>(), element.GetComponent<RectTransform>());

			Selection.activeGameObject = element;
		}

		private static GameObject GetOrCreateCanvasGameObject()
		{
			GameObject selectedGo = Selection.activeGameObject;

			// Try to find a gameobject that is the selected GO or one if its parents.
			Canvas canvas = (selectedGo != null) ? selectedGo.GetComponentInParent<Canvas>() : null;
			if (canvas != null && canvas.gameObject.activeInHierarchy)
				return canvas.gameObject;

			// No canvas in selection or its parents? Then use just any canvas..
			canvas = Object.FindObjectOfType(typeof(Canvas)) as Canvas;
			if (canvas != null && canvas.gameObject.activeInHierarchy)
				return canvas.gameObject;

			// No canvas in the scene at all? Then create a new one.
			return CreateNewUI();
		}

		private static GameObject CreateNewUI()
		{
			// Root for the UI
			var root = new GameObject("Canvas");
			root.layer = LayerMask.NameToLayer(kUILayerName);
			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			root.AddComponent<CanvasScaler>();
			root.AddComponent<GraphicRaycaster>();
			Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);

			// if there is no event system add one...
			CreateEventSystem(root);
			return root;
		}

		private static void CreateEventSystem(GameObject parent)
		{
			var esys = Object.FindObjectOfType<EventSystem>();
			if (esys == null)
			{
				var eventSystem = new GameObject("EventSystem");
				GameObjectUtility.SetParentAndAlign(eventSystem, parent);
				eventSystem.AddComponent<EventSystem>();
				eventSystem.AddComponent<StandaloneInputModule>();

				Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
			}
		}

		private static GameObject CreateButton(DefaultControls.Resources resources)
		{
			GameObject buttonRoot = CreateUIElementRoot("Button 2.0", s_ThickElementSize);

			GameObject childText = new GameObject("Text");
			childText.AddComponent<RectTransform>();
			GameObjectUtility.SetParentAndAlign(childText, buttonRoot);

			Image image = buttonRoot.AddComponent<Image>();
			image.sprite = resources.standard;
			image.type = Image.Type.Sliced;
			image.color = s_DefaultSelectableColor;

			Button2 bt = buttonRoot.AddComponent<Button2>();
			SetDefaultColorTransitionValues(bt);

			Text text = childText.AddComponent<Text>();
			text.text = "Button 2.0";
			text.alignment = TextAnchor.MiddleCenter;
			SetDefaultTextValues(text);

			RectTransform textRectTransform = childText.GetComponent<RectTransform>();
			textRectTransform.anchorMin = Vector2.zero;
			textRectTransform.anchorMax = Vector2.one;
			textRectTransform.sizeDelta = Vector2.zero;

			return buttonRoot;
		}

		private static GameObject CreateToggle(DefaultControls.Resources resources)
		{
			// Set up hierarchy
			GameObject toggleRoot = CreateUIElementRoot("Toggle 2.0", s_ThinElementSize);

			GameObject background = CreateUIObject("Background", toggleRoot);
			GameObject checkmark = CreateUIObject("Checkmark", background);
			GameObject childLabel = CreateUIObject("Label", toggleRoot);

			// Set up components
			Toggle2 toggle = toggleRoot.AddComponent<Toggle2>();
			toggle.isOn = true;

			Image bgImage = background.AddComponent<Image>();
			bgImage.sprite = resources.standard;
			bgImage.type = Image.Type.Sliced;
			bgImage.color = s_DefaultSelectableColor;

			Image checkmarkImage = checkmark.AddComponent<Image>();
			checkmarkImage.sprite = resources.checkmark;

			Text label = childLabel.AddComponent<Text>();
			label.text = "Toggle 2.0";
			SetDefaultTextValues(label);

			toggle.graphic = checkmarkImage;
			toggle.targetGraphic = bgImage;
			SetDefaultColorTransitionValues(toggle);

			RectTransform bgRect = background.GetComponent<RectTransform>();
			bgRect.anchorMin = new Vector2(0f, 1f);
			bgRect.anchorMax = new Vector2(0f, 1f);
			bgRect.anchoredPosition = new Vector2(10f, -10f);
			bgRect.sizeDelta = new Vector2(kThinHeight, kThinHeight);

			RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
			checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
			checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
			checkmarkRect.anchoredPosition = Vector2.zero;
			checkmarkRect.sizeDelta = new Vector2(20f, 20f);

			RectTransform labelRect = childLabel.GetComponent<RectTransform>();
			labelRect.anchorMin = new Vector2(0f, 0f);
			labelRect.anchorMax = new Vector2(1f, 1f);
			labelRect.offsetMin = new Vector2(23f, 1f);
			labelRect.offsetMax = new Vector2(-5f, -2f);

			return toggleRoot;
		}

		private static GameObject CreateSlider(DefaultControls.Resources resources)
		{
			// Create GOs Hierarchy
			GameObject root = CreateUIElementRoot("Slider 2.0", s_ThinElementSize);

			GameObject background = CreateUIObject("Background", root);
			GameObject fillArea = CreateUIObject("Fill Area", root);
			GameObject fill = CreateUIObject("Fill", fillArea);
			GameObject handleArea = CreateUIObject("Handle Slide Area", root);
			GameObject handle = CreateUIObject("Handle", handleArea);

			// Background
			Image backgroundImage = background.AddComponent<Image>();
			backgroundImage.sprite = resources.background;
			backgroundImage.type = Image.Type.Sliced;
			backgroundImage.color = s_DefaultSelectableColor;
			RectTransform backgroundRect = background.GetComponent<RectTransform>();
			backgroundRect.anchorMin = new Vector2(0, 0.25f);
			backgroundRect.anchorMax = new Vector2(1, 0.75f);
			backgroundRect.sizeDelta = new Vector2(0, 0);

			// Fill Area
			RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
			fillAreaRect.anchorMin = new Vector2(0, 0.25f);
			fillAreaRect.anchorMax = new Vector2(1, 0.75f);
			fillAreaRect.anchoredPosition = new Vector2(-5, 0);
			fillAreaRect.sizeDelta = new Vector2(-20, 0);

			// Fill
			Image fillImage = fill.AddComponent<Image>();
			fillImage.sprite = resources.standard;
			fillImage.type = Image.Type.Sliced;
			fillImage.color = s_DefaultSelectableColor;

			RectTransform fillRect = fill.GetComponent<RectTransform>();
			fillRect.sizeDelta = new Vector2(10, 0);

			// Handle Area
			RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
			handleAreaRect.sizeDelta = new Vector2(-20, 0);
			handleAreaRect.anchorMin = new Vector2(0, 0);
			handleAreaRect.anchorMax = new Vector2(1, 1);

			// Handle
			Image handleImage = handle.AddComponent<Image>();
			handleImage.sprite = resources.knob;
			handleImage.color = s_DefaultSelectableColor;

			RectTransform handleRect = handle.GetComponent<RectTransform>();
			handleRect.sizeDelta = new Vector2(20, 0);

			// Setup slider component
			Slider2 slider = root.AddComponent<Slider2>();
			slider.fillRect = fill.GetComponent<RectTransform>();
			slider.handleRect = handle.GetComponent<RectTransform>();
			slider.targetGraphic = handleImage;
			slider.direction = Slider.Direction.LeftToRight;
			SetDefaultColorTransitionValues(slider);

			return root;
		}

		private static GameObject CreateScrollbar(DefaultControls.Resources resources)
		{
			// Create GOs Hierarchy
			GameObject scrollbarRoot = CreateUIElementRoot("Scrollbar 2.0", s_ThinElementSize);

			GameObject sliderArea = CreateUIObject("Sliding Area", scrollbarRoot);
			GameObject handle = CreateUIObject("Handle", sliderArea);

			Image bgImage = scrollbarRoot.AddComponent<Image>();
			bgImage.sprite = resources.background;
			bgImage.type = Image.Type.Sliced;
			bgImage.color = s_DefaultSelectableColor;

			Image handleImage = handle.AddComponent<Image>();
			handleImage.sprite = resources.standard;
			handleImage.type = Image.Type.Sliced;
			handleImage.color = s_DefaultSelectableColor;

			RectTransform sliderAreaRect = sliderArea.GetComponent<RectTransform>();
			sliderAreaRect.sizeDelta = new Vector2(-20, -20);
			sliderAreaRect.anchorMin = Vector2.zero;
			sliderAreaRect.anchorMax = Vector2.one;

			RectTransform handleRect = handle.GetComponent<RectTransform>();
			handleRect.sizeDelta = new Vector2(20, 20);

			Scrollbar2 scrollbar = scrollbarRoot.AddComponent<Scrollbar2>();
			scrollbar.handleRect = handleRect;
			scrollbar.targetGraphic = handleImage;
			SetDefaultColorTransitionValues(scrollbar);

			return scrollbarRoot;
		}

		private static GameObject CreateDropdown(DefaultControls.Resources resources)
		{
			GameObject root = CreateUIElementRoot("Dropdown 2.0", s_ThickElementSize);

			GameObject label = CreateUIObject("Label", root);
			GameObject arrow = CreateUIObject("Arrow", root);
			GameObject template = CreateUIObject("Template", root);
			GameObject viewport = CreateUIObject("Viewport", template);
			GameObject content = CreateUIObject("Content", viewport);
			GameObject item = CreateUIObject("Item", content);
			GameObject itemBackground = CreateUIObject("Item Background", item);
			GameObject itemCheckmark = CreateUIObject("Item Checkmark", item);
			GameObject itemLabel = CreateUIObject("Item Label", item);

			// Sub controls.

			GameObject scrollbar = CreateScrollbar(resources);
			scrollbar.name = "Scrollbar";
			GameObjectUtility.SetParentAndAlign(scrollbar, template);

			Scrollbar2 scrollbarScrollbar = scrollbar.GetComponent<Scrollbar2>();
			scrollbarScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

			RectTransform vScrollbarRT = scrollbar.GetComponent<RectTransform>();
			vScrollbarRT.anchorMin = Vector2.right;
			vScrollbarRT.anchorMax = Vector2.one;
			vScrollbarRT.pivot = Vector2.one;
			vScrollbarRT.sizeDelta = new Vector2(vScrollbarRT.sizeDelta.x, 0);

			// Setup item UI components.

			Text itemLabelText = itemLabel.AddComponent<Text>();
			SetDefaultTextValues(itemLabelText);
			itemLabelText.alignment = TextAnchor.MiddleLeft;

			Image itemBackgroundImage = itemBackground.AddComponent<Image>();
			itemBackgroundImage.color = new Color32(245, 245, 245, 255);

			Image itemCheckmarkImage = itemCheckmark.AddComponent<Image>();
			itemCheckmarkImage.sprite = resources.checkmark;

			Toggle2 itemToggle = item.AddComponent<Toggle2>();
			itemToggle.targetGraphic = itemBackgroundImage;
			itemToggle.graphic = itemCheckmarkImage;
			itemToggle.isOn = true;

			// Setup template UI components.

			Image templateImage = template.AddComponent<Image>();
			templateImage.sprite = resources.standard;
			templateImage.type = Image.Type.Sliced;

			ScrollRect2 templateScrollRect = template.AddComponent<ScrollRect2>();
			templateScrollRect.content = (RectTransform)content.transform;
			templateScrollRect.viewport = (RectTransform)viewport.transform;
			templateScrollRect.horizontal = false;
			templateScrollRect.movementType = ScrollRect.MovementType.Clamped;
			templateScrollRect.verticalScrollbar = scrollbarScrollbar;
			templateScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			templateScrollRect.verticalScrollbarSpacing = -3;

			Mask scrollRectMask = viewport.AddComponent<Mask>();
			scrollRectMask.showMaskGraphic = false;

			Image viewportImage = viewport.AddComponent<Image>();
			viewportImage.sprite = resources.mask;
			viewportImage.type = Image.Type.Sliced;

			// Setup dropdown UI components.

			Text labelText = label.AddComponent<Text>();
			SetDefaultTextValues(labelText);
			labelText.alignment = TextAnchor.MiddleLeft;

			Image arrowImage = arrow.AddComponent<Image>();
			arrowImage.sprite = resources.dropdown;

			Image backgroundImage = root.AddComponent<Image>();
			backgroundImage.sprite = resources.standard;
			backgroundImage.color = s_DefaultSelectableColor;
			backgroundImage.type = Image.Type.Sliced;

			Dropdown2 dropdown = root.AddComponent<Dropdown2>();
			dropdown.targetGraphic = backgroundImage;
			SetDefaultColorTransitionValues(dropdown);
			dropdown.template = template.GetComponent<RectTransform>();
			dropdown.captionText = labelText;
			dropdown.itemText = itemLabelText;

			// Setting default Item list.
			itemLabelText.text = "Option A";
			dropdown.options.Add(new Dropdown.OptionData { text = "Option A" });
			dropdown.options.Add(new Dropdown.OptionData { text = "Option B" });
			dropdown.options.Add(new Dropdown.OptionData { text = "Option C" });
			dropdown.RefreshShownValue();

			// Set up RectTransforms.

			RectTransform labelRT = label.GetComponent<RectTransform>();
			labelRT.anchorMin = Vector2.zero;
			labelRT.anchorMax = Vector2.one;
			labelRT.offsetMin = new Vector2(10, 6);
			labelRT.offsetMax = new Vector2(-25, -7);

			RectTransform arrowRT = arrow.GetComponent<RectTransform>();
			arrowRT.anchorMin = new Vector2(1, 0.5f);
			arrowRT.anchorMax = new Vector2(1, 0.5f);
			arrowRT.sizeDelta = new Vector2(20, 20);
			arrowRT.anchoredPosition = new Vector2(-15, 0);

			RectTransform templateRT = template.GetComponent<RectTransform>();
			templateRT.anchorMin = new Vector2(0, 0);
			templateRT.anchorMax = new Vector2(1, 0);
			templateRT.pivot = new Vector2(0.5f, 1);
			templateRT.anchoredPosition = new Vector2(0, 2);
			templateRT.sizeDelta = new Vector2(0, 150);

			RectTransform viewportRT = viewport.GetComponent<RectTransform>();
			viewportRT.anchorMin = new Vector2(0, 0);
			viewportRT.anchorMax = new Vector2(1, 1);
			viewportRT.sizeDelta = new Vector2(-18, 0);
			viewportRT.pivot = new Vector2(0, 1);

			RectTransform contentRT = content.GetComponent<RectTransform>();
			contentRT.anchorMin = new Vector2(0f, 1);
			contentRT.anchorMax = new Vector2(1f, 1);
			contentRT.pivot = new Vector2(0.5f, 1);
			contentRT.anchoredPosition = new Vector2(0, 0);
			contentRT.sizeDelta = new Vector2(0, 28);

			RectTransform itemRT = item.GetComponent<RectTransform>();
			itemRT.anchorMin = new Vector2(0, 0.5f);
			itemRT.anchorMax = new Vector2(1, 0.5f);
			itemRT.sizeDelta = new Vector2(0, 20);

			RectTransform itemBackgroundRT = itemBackground.GetComponent<RectTransform>();
			itemBackgroundRT.anchorMin = Vector2.zero;
			itemBackgroundRT.anchorMax = Vector2.one;
			itemBackgroundRT.sizeDelta = Vector2.zero;

			RectTransform itemCheckmarkRT = itemCheckmark.GetComponent<RectTransform>();
			itemCheckmarkRT.anchorMin = new Vector2(0, 0.5f);
			itemCheckmarkRT.anchorMax = new Vector2(0, 0.5f);
			itemCheckmarkRT.sizeDelta = new Vector2(20, 20);
			itemCheckmarkRT.anchoredPosition = new Vector2(10, 0);

			RectTransform itemLabelRT = itemLabel.GetComponent<RectTransform>();
			itemLabelRT.anchorMin = Vector2.zero;
			itemLabelRT.anchorMax = Vector2.one;
			itemLabelRT.offsetMin = new Vector2(20, 1);
			itemLabelRT.offsetMax = new Vector2(-10, -2);

			template.SetActive(false);

			return root;
		}

		private static GameObject CreateInputField(DefaultControls.Resources resources)
		{
			GameObject root = CreateUIElementRoot("InputField 2.0", s_ThickElementSize);

			GameObject childPlaceholder = CreateUIObject("Placeholder", root);
			GameObject childText = CreateUIObject("Text", root);

			Image image = root.AddComponent<Image>();
			image.sprite = resources.inputField;
			image.type = Image.Type.Sliced;
			image.color = s_DefaultSelectableColor;

			InputField2 inputField = root.AddComponent<InputField2>();
			SetDefaultColorTransitionValues(inputField);

			Text text = childText.AddComponent<Text>();
			text.text = "";
			text.supportRichText = false;
			SetDefaultTextValues(text);

			Text placeholder = childPlaceholder.AddComponent<Text>();
			placeholder.text = "Enter text...";
			placeholder.fontStyle = FontStyle.Italic;
			// Make placeholder color half as opaque as normal text color.
			Color placeholderColor = text.color;
			placeholderColor.a *= 0.5f;
			placeholder.color = placeholderColor;

			RectTransform textRectTransform = childText.GetComponent<RectTransform>();
			textRectTransform.anchorMin = Vector2.zero;
			textRectTransform.anchorMax = Vector2.one;
			textRectTransform.sizeDelta = Vector2.zero;
			textRectTransform.offsetMin = new Vector2(10, 6);
			textRectTransform.offsetMax = new Vector2(-10, -7);

			RectTransform placeholderRectTransform = childPlaceholder.GetComponent<RectTransform>();
			placeholderRectTransform.anchorMin = Vector2.zero;
			placeholderRectTransform.anchorMax = Vector2.one;
			placeholderRectTransform.sizeDelta = Vector2.zero;
			placeholderRectTransform.offsetMin = new Vector2(10, 6);
			placeholderRectTransform.offsetMax = new Vector2(-10, -7);

			inputField.textComponent = text;
			inputField.placeholder = placeholder;

			return root;
		}

		private static GameObject CreateScrollView(DefaultControls.Resources resources)
		{
			GameObject root = CreateUIElementRoot("Scroll View 2.0", new Vector2(200, 200));

			GameObject viewport = CreateUIObject("Viewport", root);
			GameObject content = CreateUIObject("Content", viewport);

			// Sub controls.

			GameObject hScrollbar = CreateScrollbar(resources);
			hScrollbar.name = "Scrollbar Horizontal";
			GameObjectUtility.SetParentAndAlign(hScrollbar, root);
			RectTransform hScrollbarRT = hScrollbar.GetComponent<RectTransform>();
			hScrollbarRT.anchorMin = Vector2.zero;
			hScrollbarRT.anchorMax = Vector2.right;
			hScrollbarRT.pivot = Vector2.zero;
			hScrollbarRT.sizeDelta = new Vector2(0, hScrollbarRT.sizeDelta.y);

			GameObject vScrollbar = CreateScrollbar(resources);
			vScrollbar.name = "Scrollbar Vertical";
			GameObjectUtility.SetParentAndAlign(vScrollbar, root);
			vScrollbar.GetComponent<Scrollbar2>().SetDirection(Scrollbar.Direction.BottomToTop, true);
			RectTransform vScrollbarRT = vScrollbar.GetComponent<RectTransform>();
			vScrollbarRT.anchorMin = Vector2.right;
			vScrollbarRT.anchorMax = Vector2.one;
			vScrollbarRT.pivot = Vector2.one;
			vScrollbarRT.sizeDelta = new Vector2(vScrollbarRT.sizeDelta.x, 0);

			// Setup RectTransforms.

			// Make viewport fill entire scroll view.
			RectTransform viewportRT = viewport.GetComponent<RectTransform>();
			viewportRT.anchorMin = Vector2.zero;
			viewportRT.anchorMax = Vector2.one;
			viewportRT.sizeDelta = Vector2.zero;
			viewportRT.pivot = Vector2.up;

			// Make context match viewpoprt width and be somewhat taller.
			// This will show the vertical scrollbar and not the horizontal one.
			RectTransform contentRT = content.GetComponent<RectTransform>();
			contentRT.anchorMin = Vector2.up;
			contentRT.anchorMax = Vector2.one;
			contentRT.sizeDelta = new Vector2(0, 300);
			contentRT.pivot = Vector2.up;

			// Setup UI components.

			ScrollRect2 scrollRect = root.AddComponent<ScrollRect2>();
			scrollRect.content = contentRT;
			scrollRect.viewport = viewportRT;
			scrollRect.horizontalScrollbar = hScrollbar.GetComponent<Scrollbar2>();
			scrollRect.verticalScrollbar = vScrollbar.GetComponent<Scrollbar2>();
			scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			scrollRect.horizontalScrollbarSpacing = -3;
			scrollRect.verticalScrollbarSpacing = -3;

			Image rootImage = root.AddComponent<Image>();
			rootImage.sprite = resources.background;
			rootImage.type = Image.Type.Sliced;
			rootImage.color = s_PanelColor;

			Mask viewportMask = viewport.AddComponent<Mask>();
			viewportMask.showMaskGraphic = false;

			Image viewportImage = viewport.AddComponent<Image>();
			viewportImage.sprite = resources.mask;
			viewportImage.type = Image.Type.Sliced;

			return root;
		}

		private static GameObject CreateUIElementRoot(string name, Vector2 size)
		{
			GameObject child = new GameObject(name);
			RectTransform rectTransform = child.AddComponent<RectTransform>();
			rectTransform.sizeDelta = size;
			return child;
		}

		private static GameObject CreateUIObject(string name, GameObject parent)
		{
			GameObject go = new GameObject(name);
			go.AddComponent<RectTransform>();
			GameObjectUtility.SetParentAndAlign(go, parent);
			return go;
		}

		private static void SetDefaultColorTransitionValues(Selectable slider)
		{
			ColorBlock colors = slider.colors;
			colors.highlightedColor = new Color(0.882f, 0.882f, 0.882f);
			colors.pressedColor = new Color(0.698f, 0.698f, 0.698f);
			colors.disabledColor = new Color(0.521f, 0.521f, 0.521f);
		}

		private static void SetDefaultTextValues(Text lbl)
		{
			// Set text values we want across UI elements in default controls.
			// Don't set values which are the same as the default values for the Text component,
			// since there's no point in that, and it's good to keep them as consistent as possible.
			lbl.color = s_TextColor;

			// Reset() is not called when playing. We still want the default font to be assigned
			lbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		}

		private static void SetPositionVisibleinSceneView(RectTransform canvasRTransform, RectTransform itemTransform)
		{
			SceneView sceneView = SceneView.lastActiveSceneView;

			// Couldn't find a SceneView. Don't set position.
			if (sceneView == null || sceneView.camera == null)
				return;

			// Create world space Plane from canvas position.
			Vector2 localPlanePosition;
			Camera camera = sceneView.camera;
			Vector3 position = Vector3.zero;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRTransform, new Vector2(camera.pixelWidth / 2, camera.pixelHeight / 2), camera, out localPlanePosition))
			{
				// Adjust for canvas pivot
				localPlanePosition.x += canvasRTransform.sizeDelta.x * canvasRTransform.pivot.x;
				localPlanePosition.y += canvasRTransform.sizeDelta.y * canvasRTransform.pivot.y;

				localPlanePosition.x = Mathf.Clamp(localPlanePosition.x, 0, canvasRTransform.sizeDelta.x);
				localPlanePosition.y = Mathf.Clamp(localPlanePosition.y, 0, canvasRTransform.sizeDelta.y);

				// Adjust for anchoring
				position.x = localPlanePosition.x - canvasRTransform.sizeDelta.x * itemTransform.anchorMin.x;
				position.y = localPlanePosition.y - canvasRTransform.sizeDelta.y * itemTransform.anchorMin.y;

				Vector3 minLocalPosition;
				minLocalPosition.x = canvasRTransform.sizeDelta.x * (0 - canvasRTransform.pivot.x) + itemTransform.sizeDelta.x * itemTransform.pivot.x;
				minLocalPosition.y = canvasRTransform.sizeDelta.y * (0 - canvasRTransform.pivot.y) + itemTransform.sizeDelta.y * itemTransform.pivot.y;

				Vector3 maxLocalPosition;
				maxLocalPosition.x = canvasRTransform.sizeDelta.x * (1 - canvasRTransform.pivot.x) - itemTransform.sizeDelta.x * itemTransform.pivot.x;
				maxLocalPosition.y = canvasRTransform.sizeDelta.y * (1 - canvasRTransform.pivot.y) - itemTransform.sizeDelta.y * itemTransform.pivot.y;

				position.x = Mathf.Clamp(position.x, minLocalPosition.x, maxLocalPosition.x);
				position.y = Mathf.Clamp(position.y, minLocalPosition.y, maxLocalPosition.y);
			}

			itemTransform.anchoredPosition = position;
			itemTransform.localRotation = Quaternion.identity;
			itemTransform.localScale = Vector3.one;
		}
	}
}