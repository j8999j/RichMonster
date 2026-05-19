using System.Collections.Generic;
using GameSystem;

public class Task3_ThirdTutorial : GuideTask
{
    public override string TaskName => "教學三：查看訂單";

    private const string WaitHumanDayStepId = "Task3.WaitHumanDay";
    private const string WaitHumanSceneStepId = "Task3.WaitHumanScene";
    private const string WaitFrameStepId = "Task3.WaitFrame";
    private const string OrderShopGuideStepId = "Task3.OrderShopGuide";

    protected override IReadOnlyList<string> StepIds => new[]
    {
        WaitHumanDayStepId,
        WaitHumanSceneStepId,
        WaitFrameStepId,
        OrderShopGuideStepId
    };

    protected override List<GuideStep> BuildSteps()
    {
        return new List<GuideStep>
        {
            new WaitForDayPhaseStep(1, DayPhase.HumanDay),
            new WaitForSceneStep(SceneTransitionManager.SCENE_HUMAN),
            new WaitForFramesStep(1),
            new WithMapGuideStep(
                inner: new ShowHintAndWaitStep(
                    "前往爺爺的雜貨店查看訂單",
                    new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop)),
                targetId: GuideIDs.Interactable.GuideOrderShop)
        };
    }
}
