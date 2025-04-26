using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Command.Compelete;

public sealed record CompleteTodoCommand(Guid TodoItemId):ICommands;

