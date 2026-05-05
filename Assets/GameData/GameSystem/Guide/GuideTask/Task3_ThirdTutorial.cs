using System.Collections.Generic;
using GameSystem;

public class Task3_ThirdTutorial : GuideTask
{
    public override string TaskName => "教學三：查看訂單";

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
