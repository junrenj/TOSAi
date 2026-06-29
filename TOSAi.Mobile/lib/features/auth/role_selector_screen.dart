import 'package:flutter/material.dart';

import '../../core/api/platform_api_client.dart';
import '../../core/models/user_role.dart';
import '../shell/role_shell.dart';

class RoleSelectorScreen extends StatelessWidget {
  const RoleSelectorScreen({
    required this.apiClient,
    super.key,
  });

  final PlatformApiClient apiClient;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 520),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'TOS AI',
                    style: Theme.of(context).textTheme.displaySmall?.copyWith(
                          fontWeight: FontWeight.w800,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '选择当前使用端',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 28),
                  _RoleCard(
                    title: '学生端',
                    subtitle: '查看今日任务、作业、学习进度和拍照搜题入口',
                    icon: Icons.school_outlined,
                    onTap: () => _openRole(context, UserRole.student),
                  ),
                  const SizedBox(height: 14),
                  _RoleCard(
                    title: '家长端',
                    subtitle: '查看孩子成绩趋势、学习报告和心理关注提醒',
                    icon: Icons.family_restroom_outlined,
                    onTap: () => _openRole(context, UserRole.parent),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  void _openRole(BuildContext context, UserRole role) {
    Navigator.of(context).pushReplacement(
      MaterialPageRoute<void>(
        builder: (_) => RoleShell(apiClient: apiClient, role: role),
      ),
    );
  }
}

class _RoleCard extends StatelessWidget {
  const _RoleCard({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.onTap,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.primaryContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: Theme.of(context).colorScheme.primary),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
                    const SizedBox(height: 5),
                    Text(subtitle, style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.black54)),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
