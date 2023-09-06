using Elements;
using Elements.Components;
using Elements.Geometry;
using LayoutFunctionCommon;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SpaceType
{
  public static class SpaceType
  {
    /// <summary>
    /// The SpaceType function.
    /// </summary>
    /// <param name="model">The input model.</param>
    /// <param name="input">The arguments to the execution.</param>
    /// <returns>A SpaceTypeOutputs instance containing computed results and the model with any new elements.</returns>
    public static SpaceTypeOutputs Execute(Dictionary<string, Model> inputModels, SpaceTypeInputs input)
    {
      Elements.Serialization.glTF.GltfExtensions.UseReferencedContentExtension = true;
      var output = new SpaceTypeOutputs();

      if (!inputModels.TryGetValue("Program Requirements", out var programReqModel))
      {
        output.Errors.Add("Missing input model: Program Requirements");
        return output;
      }

      var programReqs = programReqModel.AllElementsOfType<ProgramRequirement>();

      foreach (var programReq in programReqs)
      {
        var programName = programReq.QualifiedProgramName;
        var (catalogPath, configPath) = programReq.WriteLayoutConfigs(programReqModel);
        if (catalogPath == null || configPath == null)
        {
          continue;
        }
        LayoutStrategies.StandardLayoutOnAllLevels<LevelElements, LevelVolume, SpaceBoundary, CirculationSegment>(
            programName,
            inputModels,
            input.Overrides,
            output.Model,
            true,
            configPath,
            catalogPath);
      }

      return output;
    }
  }
}