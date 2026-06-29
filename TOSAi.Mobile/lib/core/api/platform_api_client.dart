import '../models/platform_feature_page.dart';
import '../models/user_role.dart';

abstract interface class PlatformApiClient {
  Future<PlatformFeaturePage> getFeaturePage({
    required UserRole role,
    required String pageKey,
  });
}
