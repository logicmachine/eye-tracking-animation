using System.Linq;

using nadena.dev.ndmf;

using dev.logilabo.eye_tracking_animation.editor;
using dev.logilabo.eye_tracking_animation.runtime;
using dev.logilabo.parameter_smoother.runtime;

[assembly: ExportsPlugin(typeof(EyeTrackingSmootherPlugin))]

// ReSharper disable once CheckNamespace
namespace dev.logilabo.eye_tracking_animation.editor
{
    public class EyeTrackingSmootherPlugin : Plugin<EyeTrackingSmootherPlugin>
    {
        public override string DisplayName => "EyeTrackingSmoother";
        public override string QualifiedName => "dev.logilabo.eye-tracking-controller.smoother";

        protected override void Configure()
        {
            var prefix = "EyeTrackingController/v2/";
            var parameters = new[]
            {
                prefix + "LInOut",
                prefix + "RInOut",
                prefix + "Pitch",
            };
            InPhase(BuildPhase.Generating)
                .Run("Generate Parameter Smoother", ctx =>
                {
                    var components = ctx.AvatarRootObject.GetComponentsInChildren<EyeTrackingController>();
                    foreach (var component in components)
                    {
                        var layers = component.gazeMotions
                            .Select(s => s.layerType)
                            .Distinct();
                        foreach (var layer in layers)
                        {
                            var smoother = component.gameObject.AddComponent<ParameterSmoother>();
                            smoother.layerType = layer;
                            smoother.smoothedSuffix = "/Smoothed";
                            smoother.configs = parameters
                                .Select(p => new SmoothingConfig
                                {
                                    parameterName = p,
                                    localSmoothness = component.localSmoothness,
                                    remoteSmoothness = component.remoteSmoothness,
                                })
                                .ToList();
                        }
                    }
                })
                .BeforePlugin("dev.logilabo.parameter-smoother");
        }
    }
}
