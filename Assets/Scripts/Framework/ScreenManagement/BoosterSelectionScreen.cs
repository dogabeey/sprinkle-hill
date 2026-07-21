using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.EventManagement;

namespace Game
{
    /// <summary>Lets the player choose one of the power-ups enabled for the active level.</summary>
    public class BoosterSelectionScreen : GameScreen
    {
        public override Screens ScreenID => Screens.BoosterSelection;

        [Min(1)] public int replacementCount = 2;

        [Header("Boosters")]
        public BoosterUINode boosterNodePrefab;
        public GridLayoutGroup boosterGridLayout;

        [Header("Objectives")]
        public ObjectiveUINode objectiveNodePrefab;
        public Transform objectivesContainer;

        private readonly List<BoosterUINode> boosterNodes = new List<BoosterUINode>();
        private readonly List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
            EventManager.StartListening(GameEvent.OBJECTIVE_PROGRESS_UPDATED, OnObjectiveProgressUpdated);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
            EventManager.StopListening(GameEvent.OBJECTIVE_PROGRESS_UPDATED, OnObjectiveProgressUpdated);
        }

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            InstantiateBoosterNodes();
            InstantiateObjectiveNodes();
        }

        public override void ResolveParams(EventParam eventParam)
        {
        }

        private void InstantiateBoosterNodes()
        {
            if (boosterGridLayout != null)
            {
                foreach (Transform child in boosterGridLayout.transform)
                {
                    // Destroy is deferred until the end of the frame, so disable first to ensure
                    // old nodes cannot briefly appear when the panel is opened.
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
            boosterNodes.Clear();

            LevelScene_Match3Game level = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            if (boosterNodePrefab == null || boosterGridLayout == null || level == null)
                return;

            AddBoosterNode(ElementPowerUpType.DiscoBall, level.AllowDiscoBallCreation);
            AddBoosterNode(ElementPowerUpType.Rocket, level.AllowRocketCreation);
            AddBoosterNode(ElementPowerUpType.Bomb, level.AllowBombCreation);
            AddBoosterNode(ElementPowerUpType.Propeller, level.AllowPropellerCreation);
        }

        private void AddBoosterNode(ElementPowerUpType powerUpType, bool isEnabledForLevel)
        {
            if (!isEnabledForLevel)
                return;

            BoosterUINode node = Instantiate(boosterNodePrefab, boosterGridLayout.transform);
            node.Initialize(powerUpType, SelectBooster);
            boosterNodes.Add(node);
        }

        private void InstantiateObjectiveNodes()
        {
            foreach (ObjectiveUINode node in objectiveNodes)
            {
                if (node != null)
                    Destroy(node.gameObject);
            }
            objectiveNodes.Clear();

            ObjectiveManager objectiveManager = ObjectiveManager.Instance;
            if (objectiveNodePrefab == null || objectivesContainer == null || objectiveManager == null || objectiveManager.activeObjectives == null)
                return;

            foreach (Objective objective in objectiveManager.activeObjectives)
            {
                if (objective == null || objective.tiedToLockedArea)
                    continue;

                ObjectiveUINode node = Instantiate(objectiveNodePrefab, objectivesContainer);
                node.Initialize(objective);
                node.UpdateNode(objectiveManager.GetCurrentCount(objective));
                objectiveNodes.Add(node);
            }
        }

        private void UpdateObjectiveNodes()
        {
            ObjectiveManager objectiveManager = ObjectiveManager.Instance;
            if (objectiveManager == null)
                return;

            foreach (ObjectiveUINode node in objectiveNodes)
            {
                if (node != null && node.referenceObjective != null)
                    node.UpdateNode(objectiveManager.GetCurrentCount(node.referenceObjective));
            }
        }

        private void OnObjectivesInitialized(EventParam eventParam)
        {
            InstantiateObjectiveNodes();
        }

        private void OnObjectiveProgressUpdated(EventParam eventParam)
        {
            UpdateObjectiveNodes();
        }

        private void SelectBooster(ElementPowerUpType selectedPowerUp)
        {
            if (selectedPowerUp == ElementPowerUpType.None)
                return;

            Match3Grid grid = (GameManager.Instance.CurrentLevel as LevelScene_Match3Game)?.grid as Match3Grid;
            if (grid == null)
            {
                Debug.LogWarning("Cannot apply bonus booster because there is no active Match3Grid.");
                return;
            }

            int replacedElementCount = grid.ReplaceRandomRegularElementsWithPowerUp(selectedPowerUp, replacementCount);
            if (replacedElementCount > 0)
                ScreenManager.Instance.CloseAllScreens();
        }
    }
}
