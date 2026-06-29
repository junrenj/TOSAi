import 'package:flutter/material.dart';

import '../../core/api/platform_api_client.dart';
import '../../core/models/user_role.dart';
import '../auth/role_selector_screen.dart';
import '../shared/feature_page_screen.dart';

class RoleShell extends StatefulWidget {
  const RoleShell({
    required this.apiClient,
    required this.role,
    super.key,
  });

  final PlatformApiClient apiClient;
  final UserRole role;

  @override
  State<RoleShell> createState() => _RoleShellState();
}

class _RoleShellState extends State<RoleShell> {
  int _selectedIndex = 0;

  late final List<_Destination> _destinations = switch (widget.role) {
    UserRole.student => const [
        _Destination('首页', Icons.home_outlined, 'studentHome'),
        _Destination('作业', Icons.assignment_outlined, 'studentHomework'),
        _Destination('进度', Icons.trending_up_outlined, 'studentProgress'),
        _Destination('搜题', Icons.photo_camera_outlined, 'studentPhotoQuestion'),
      ],
    UserRole.parent => const [
        _Destination('首页', Icons.home_outlined, 'parentHome'),
        _Destination('趋势', Icons.show_chart_outlined, 'parentTrends'),
        _Destination('报告', Icons.article_outlined, 'parentReports'),
        _Destination('关注', Icons.favorite_border_outlined, 'parentWellbeing'),
      ],
  };

  @override
  Widget build(BuildContext context) {
    final destination = _destinations[_selectedIndex];

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.role.label, style: const TextStyle(fontWeight: FontWeight.w800)),
            Text(destination.label, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black54)),
          ],
        ),
        actions: [
          IconButton(
            tooltip: '切换端',
            icon: const Icon(Icons.switch_account_outlined),
            onPressed: () {
              Navigator.of(context).pushReplacement(
                MaterialPageRoute<void>(
                  builder: (_) => RoleSelectorScreen(apiClient: widget.apiClient),
                ),
              );
            },
          ),
        ],
      ),
      body: FeaturePageScreen(
        key: ValueKey('${widget.role.name}-${destination.pageKey}'),
        apiClient: widget.apiClient,
        role: widget.role,
        pageKey: destination.pageKey,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (index) => setState(() => _selectedIndex = index),
        destinations: [
          for (final item in _destinations)
            NavigationDestination(
              icon: Icon(item.icon),
              selectedIcon: Icon(item.icon),
              label: item.label,
            ),
        ],
      ),
    );
  }
}

class _Destination {
  const _Destination(this.label, this.icon, this.pageKey);

  final String label;
  final IconData icon;
  final String pageKey;
}

