# The TURN key for face calls is not managed here. cloudflare_calls_turn_app
# in provider 5.x drops both the key and its key_id after create, so Terraform
# can neither hand the secret to the Worker nor refresh or delete the key.
# The key "friendscorner-api-faces" was created through the Calls API and its
# secret lives only in the Worker secrets TURN_KEY_ID and TURN_KEY_API_TOKEN.
