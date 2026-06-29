import 'package:flutter/material.dart';

import '../core/api/fallback_platform_api_client.dart';
import '../core/api/http_platform_api_client.dart';
import '../core/api/mock_platform_api_client.dart';
import '../core/api/platform_api_client.dart';
import '../core/theme/app_theme.dart';
import '../features/auth/role_selector_screen.dart';

class TosAiMobileApp extends StatelessWidget {
  const TosAiMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    final PlatformApiClient apiClient = FallbackPlatformApiClient(
      primary: HttpPlatformApiClient(),
      fallback: MockPlatformApiClient(),
    );

    return MaterialApp(
      title: 'TOS AI',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      home: RoleSelectorScreen(apiClient: apiClient),
    );
  }
}
