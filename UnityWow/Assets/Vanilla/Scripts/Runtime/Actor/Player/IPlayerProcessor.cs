// Copyright (c) 2026 CatRabbit. All rights reserved.

namespace CR
{
	public interface IPlayerProcessor
	{
		void OnUpdate(PlayerBlackboard blackboard, float dt);
		void OnFixedUpdate(PlayerBlackboard blackboard, float dt);
		void OnLateUpdate(PlayerBlackboard blackboard, float dt);
	}
}

