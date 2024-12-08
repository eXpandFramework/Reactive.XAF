package com.jetbrains.rider.plugins.xpand

import com.jetbrains.rider.actions.base.RiderAnAction
import com.jetbrains.rd.ui.bedsl.dsl.description
import icons.ReSharperIcons

class XpandModelEditorAction : RiderAnAction(
    backendActionId = "XpandModelEditor", // Id == CSharpClassName.TrimEnd("Action")
    // Icon must also be changed in backend code
    icon = ReSharperIcons.FeaturesInternal.QuickStartToolWindow
)
