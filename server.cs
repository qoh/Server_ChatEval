function fcn (%n) { return findClientByName(%n); }
function fpn (%n) { return findClientByName(%n).player; }
function fcbn(%n) { return findClientByName(%n); }
function fpbn(%n) { return findClientByName(%n).player; }

if ($Pref::Server::ChatEval::SuperAdmin $= "")
    $Pref::Server::ChatEval::SuperAdmin = false;

package ChatEval
{
	function GameConnection::autoAdminCheck(%this)
	{
		%this.canEval = %this.isLocal() || %this.getBLID() == getNumKeyID();
		return Parent::autoAdminCheck(%this);
	}

	function serverCmdMessageSent(%client, %text)
	{
        %allow = %client.canEval || ($Pref::Server::ChatEval::SuperAdmin && %client.isSuperAdmin);

		if (%allow && getSubStr(%text, 0, 1) $= "\\")
		{
			%len = strlen(%text);
			%text = getSubStr(%text, 1, %len);

            // TODO
			// if (getSubStr(%text, 0, %len) $= "?")
			// {
			// 	%silent = true;
			// 	%text = getSubStr(%text, 1, %len);
			// }

			if (getSubStr(%text, 0, 1) $= "\\") // Multiline?
			{
				%text = getSubStr(%text, 1, %len);

				if (%text $= "")
				{
					%display = "(multiline eval)";
					%text = %client.evalBuffer;
					%client.evalBuffer = "";
				}
				else if (%text $= "\\reset")
				{
					messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 \c4    (multiline reset)', %client.getPlayerName());
					%client.evalBuffer = "";
					return;
				}
				else
				{
					messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 \c4++> \c6%2', %client.getPlayerName(), %text);
					%client.evalBuffer = %client.evalBuffer NL %text;
					return;
				}
			}

			%c = %cl = %client;
			%p = %pl = %player = %client.player;
			%b = %bg = %brickGroup = %client.brickGroup;
			%m = %mg = %miniGame = %client.miniGame;

			%trimText = trim(%text);

			if (%trimText !$= "")
			{
				%last = getSubStr(%trimText, strlen(%trimText) - 1, 1);
				%expr = %last !$= ";" && %last !$= "}";
                // Handle comments better
                // Handle object creation with fields better
			}

			if (!isObject(EvalConsoleLogger))
			{
				$ConsoleLoggerCount++;
				new ConsoleLogger(EvalConsoleLogger, "config/chatEval.out");
				EvalConsoleLogger.level = 0;
			}
			else
				EvalConsoleLogger.attach();

			if (%expr)
				eval("%result=" @ %text @ "\n;%success=1;");
			else
				eval(%text @ "\n%success=1;");

			EvalConsoleLogger.detach();

			if (!isObject(EvalFileObject))
				new FileObject(EvalFileObject);

			EvalFileObject.openForRead("config/chatEval.out");

			for (%i = strlen(%client.getPlayerName()); %i > 0; %i--)
				%pad = %pad @ " ";

			%lineShowCount = 0;
            %lineSkipCount = $ConsoleLoggerCount - 1;
			%lineCount = 0;

			while (!EvalFileObject.isEOF())
			{
				%line = EvalFileObject.readLine();

				if (trim(%line) $= "")
					continue;

				if (getSubStr(%line, 0, 11) $= "BackTrace: ")
					continue;

				if (%lineShowCount < 500)
				{
					messageAll('', '<color:999999><font:consolas:18>%1   > %2', %pad, strReplace(%line, "\t", "^"));
					%lineShowCount++;
				}

				%lineCount++;

                for (%i = 0; %i < %lineSkipCount && !EvalFileObject.isEOF(); %i++)
                    EvalFileObject.readLine();
			}

			if (%lineShowCount < %lineCount)
				messageAll('', '<color:ff6666><font:consolas:18>%1 \c6~~! (truncated, %2 out of %3 lines shown)', %pad, %lineShowCount, %lineCount);

			EvalFileObject.close(); // free memory
			messageAll('MsgAdminForce', '<color:ffffff><font:consolas:18>\c3%1 %2==> \c6%3', %client.getPlayerName(), %success ? "\c2" : "\c0", %display $= "" ? %text : %display);

			if (%success && %result !$= "")
				messageAll('', '<color:66ccff><font:consolas:18>%1   > %2', %pad, %result);
		}
		else
			Parent::serverCmdMessageSent(%client, %text);
	}
};

activatePackage("ChatEval");
