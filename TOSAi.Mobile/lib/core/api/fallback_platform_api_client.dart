import '../models/platform_feature_page.dart';
import '../models/user_role.dart';
import 'platform_api_client.dart';

class FallbackPlatformApiClient implements PlatformApiClient {
  const FallbackPlatformApiClient({
    required this.primary,
    required this.fallback,
  });

  final PlatformApiClient primary;
  final PlatformApiClient fallback;

  @override
  Future<PlatformFeaturePage> getFeaturePage({
    required UserRole role,
    required String pageKey,
  }) async {
    try {
      return await primary.getFeaturePage(role: role, pageKey: pageKey);
    } catch (_) {
      return fallback.getFeaturePage(role: role, pageKey: pageKey);
    }
  }
}
