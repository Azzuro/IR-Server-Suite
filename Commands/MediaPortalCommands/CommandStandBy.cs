using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Util;

namespace Commands.MediaPortal
{
  /// <summary>
  /// StandBy MediaPortal macro command.
  /// </summary>
  public class CommandStandBy : Command
  {
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandStandBy"/> class.
    /// </summary>
    public CommandStandBy()
    {
      InitParameters(0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandStandBy"/> class.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    public CommandStandBy(string[] parameters) : base(parameters)
    {
    }

    #endregion Constructors

    #region Implementation

    /// <summary>
    /// Gets the category of this command.
    /// </summary>
    /// <returns>The category of this command.</returns>
    public override string GetCategory()
    {
      return "MediaPortal Commands";
    }

    /// <summary>
    /// Gets the user interface text.
    /// </summary>
    /// <returns>User interface text.</returns>
    public override string GetUserInterfaceText()
    {
      return "StandBy";
    }

    /// <summary>
    /// Execute this command.
    /// </summary>
    /// <param name="variables">The variable list of the calling code.</param>
    public override void Execute(VariableList variables)
    {
      bool mpBasicHome = false;
      using (Settings xmlreader = new Settings(Common.MPConfigFile))
        mpBasicHome = xmlreader.GetValueAsBool("general", "startbasichome", false);

      GUIGraphicsContext.ResetLastActivity();
      // Stop all media before suspending
      g_Player.Stop();

      GUIMessage msg;

      if (mpBasicHome)
        msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW, 0, 0, 0,
                             (int) GUIWindow.Window.WINDOW_SECOND_HOME, 0, null);
      else
        msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW, 0, 0, 0, (int) GUIWindow.Window.WINDOW_HOME, 0,
                             null);

      GUIWindowManager.SendThreadMessage(msg);

      WindowsController.ExitWindows(RestartOptions.Suspend, false);
    }

    #endregion Implementation
  }
}